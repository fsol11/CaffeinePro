<#
.SYNOPSIS
    Builds the MSIX package, installs it, and launches it, so Caffeine Pro can be tested exactly
    as a Store customer would get it.

.DESCRIPTION
    Builds one architecture and registers it, which needs Developer Mode (Settings > System >
    For developers) but no certificate and no administrator rights.

    A packaging project's build layout under bin\<platform>\Release is deliberately incomplete: it
    holds the entry-point application, the manifest and resources.pri, but not the image assets,
    which stay in the project folder and are pulled in by makeappx when the .msix is packed. So
    registering that layout as-is produces an app whose resources.pri points at Images\... files
    that are not on disk, and every icon - Start menu, taskbar, ALT+TAB - comes out blank. This
    script completes the layout first, copying in whatever the build recipe says belongs in the
    package but is not there yet.

    Installing the built .msix directly is not an option without a certificate: -AllowUnsigned only
    accepts publishers in Windows' unsigned namespace, which a Store identity is not. If you ever
    need to test the package file itself, rebuild with /p:AppxPackageSigningEnabled=true and a
    certificate whose subject matches the manifest Publisher exactly, trust it under
    Cert:\LocalMachine\TrustedPeople, and install the .msix from AppPackages.

.PARAMETER Platform
    Architecture to build and install: x86, x64 or arm64. Defaults to the architecture of this PC.

.PARAMETER Uninstall
    Remove an installed Caffeine Pro package and exit.

.PARAMETER NoLaunch
    Install but do not start the app.

.EXAMPLE
    .\test-package.ps1
    Build, install and launch for this PC's architecture.

.EXAMPLE
    .\test-package.ps1 -Platform arm64
    Build and install the arm64 package.

.EXAMPLE
    .\test-package.ps1 -Uninstall
    Remove the installed package.
#>

[CmdletBinding()]
param(
    [ValidateSet('x86', 'x64', 'arm64')]
    [string] $Platform,

    [switch] $Uninstall,
    [switch] $NoLaunch
)

$ErrorActionPreference = 'Stop'

# This script lives in scripts\, so everything it touches hangs off the parent directory.
$RepoRoot     = Split-Path $PSScriptRoot -Parent
$WapProject   = Join-Path $RepoRoot 'CaffeinePro Setup\CaffeinePro Setup.wapproj'
$ManifestFile = Join-Path $RepoRoot 'CaffeinePro Setup\Package.appxmanifest'
$AppProcess   = 'CaffeinePro'

function Write-Step { param([string] $Text) Write-Host "`n==> $Text" -ForegroundColor Cyan }
function Write-Note { param([string] $Text) Write-Host "    $Text" -ForegroundColor DarkGray }

# The package name is the single source of truth for what to uninstall, so it is read back out of
# the manifest rather than repeated here.
function Get-PackageName {
    [xml] $xml = Get-Content $ManifestFile
    return $xml.Package.Identity.Name
}

function Remove-InstalledPackage {
    param([string] $Name)

    Get-Process -Name $AppProcess -ErrorAction SilentlyContinue | ForEach-Object {
        Write-Note "Stopping running instance (pid $($_.Id))"
        $_ | Stop-Process -Force
    }

    $installed = Get-AppxPackage -Name $Name -ErrorAction SilentlyContinue
    if (-not $installed) { return $false }

    foreach ($pkg in $installed) {
        Write-Note "Removing $($pkg.PackageFullName)"
        Remove-AppxPackage -Package $pkg.PackageFullName
    }
    return $true
}

function Get-MSBuild {
    $vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
    if (-not (Test-Path $vswhere)) {
        throw "vswhere.exe not found. Install Visual Studio with the MSBuild component."
    }

    $msbuild = & $vswhere -latest -prerelease -requires Microsoft.Component.MSBuild `
                          -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
    if (-not $msbuild) {
        throw "MSBuild.exe not found. Install the Visual Studio MSBuild component."
    }
    return $msbuild
}

function Assert-DeveloperMode {
    $key = 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\AppModelUnlock'
    $on = (Get-ItemProperty $key -ErrorAction SilentlyContinue).AllowDevelopmentWithoutDevLicense
    if ($on -ne 1) {
        throw "Developer Mode is off. Turn it on under Settings > System > For developers."
    }
}

# Copies in every file the build recipe says belongs in the package but that the layout does not
# already contain - in practice the Images folder. Driven by the recipe rather than a hard-coded
# path so that new assets are picked up automatically.
function Complete-PackageLayout {
    param([string] $LayoutDir)

    $recipe = Get-ChildItem $LayoutDir -Filter '*.appxrecipe' | Select-Object -First 1
    if (-not $recipe) { throw "No .appxrecipe found in '$LayoutDir'." }

    [xml] $xml = Get-Content $recipe.FullName
    $ns = New-Object System.Xml.XmlNamespaceManager($xml.NameTable)
    $ns.AddNamespace('m', 'http://schemas.microsoft.com/developer/msbuild/2003')

    $copied = 0
    foreach ($file in $xml.SelectNodes('//m:AppxPackagedFile', $ns)) {
        $source = $file.Include
        $target = Join-Path $LayoutDir $file.PackagePath

        if (-not (Test-Path $source)) { continue }

        # Refresh stale copies too, or edited artwork would never reach an existing layout.
        if (Test-Path $target) {
            $srcTime = (Get-Item $source).LastWriteTimeUtc
            $dstTime = (Get-Item $target).LastWriteTimeUtc
            if ($srcTime -le $dstTime) { continue }
        }

        $dir = Split-Path $target -Parent
        if (-not (Test-Path $dir)) { New-Item -ItemType Directory -Path $dir -Force | Out-Null }
        Copy-Item $source $target
        $copied++
    }

    return $copied
}

# ---------------------------------------------------------------------------------------------

$packageName = Get-PackageName

if ($Uninstall) {
    Write-Step "Uninstalling $packageName"
    if (Remove-InstalledPackage -Name $packageName) {
        Write-Host "`nRemoved." -ForegroundColor Green
    }
    else {
        Write-Host "`nNothing to remove - the package is not installed." -ForegroundColor Yellow
    }
    return
}

if (-not $Platform) {
    $Platform = switch ($env:PROCESSOR_ARCHITECTURE) {
        'AMD64' { 'x64' }
        'ARM64' { 'arm64' }
        default { 'x86' }
    }
    Write-Note "No -Platform given, using this PC's architecture: $Platform"
}

Assert-DeveloperMode
$msbuild = Get-MSBuild

# Uninstall before building, not after. A registered package runs its executable straight out of
# the build layout, so leaving an installed copy running makes MSBuild fail with MSB3021 when it
# tries to overwrite the locked .exe.
Write-Step 'Removing any previously installed copy'
if (-not (Remove-InstalledPackage -Name $packageName)) { Write-Note 'Nothing installed' }

Write-Step "Building $Platform (Release)"
& $msbuild @(
    $WapProject
    '/restore'
    '/nologo'
    '/verbosity:minimal'
    '/p:Configuration=Release'
    "/p:Platform=$Platform"
    # One architecture, and no .msixupload: the three-architecture bundle for Partner Center is
    # what publish-store.bat produces, and building it here would triple the wait.
    '/p:AppxBundle=Never'
    '/p:UapAppxPackageBuildMode=SideloadOnly'
)
if ($LASTEXITCODE -ne 0) { throw "Build failed with exit code $LASTEXITCODE." }

$layout = Join-Path $RepoRoot "CaffeinePro Setup\bin\$Platform\Release"
if (-not (Test-Path (Join-Path $layout 'AppxManifest.xml'))) {
    throw "Package layout not found at '$layout'."
}

Write-Step 'Completing the package layout'
Write-Note "Copied $(Complete-PackageLayout -LayoutDir $layout) missing payload file(s)"

Write-Step 'Installing'
Add-AppxPackage -Register (Join-Path $layout 'AppxManifest.xml')

$installed = Get-AppxPackage -Name $packageName
if (-not $installed) { throw 'Installation reported success but the package is not registered.' }

# A package missing its image assets still installs and launches - it just renders every icon
# blank - so check for them rather than trusting the install to have caught it.
$images = Get-ChildItem (Join-Path $installed.InstallLocation 'Images') -Filter '*.png' -ErrorAction SilentlyContinue
if (-not $images) {
    Write-Warning "The installed package contains no Images folder - icons will be blank."
}
else {
    Write-Note "$($images.Count) image assets present"
}

$appId = (Get-AppxPackageManifest $installed).Package.Applications.Application.Id
$aumid = "$($installed.PackageFamilyName)!$appId"

Write-Host "`nInstalled $($installed.PackageFullName)" -ForegroundColor Green

if (-not $NoLaunch) {
    Write-Step 'Launching'
    Start-Process "shell:AppsFolder\$aumid"
}

Write-Host @"

Worth checking now:

  * Start with Windows - toggle it on in Settings, then confirm 'Caffeine Pro' appears under
    Task Manager > Startup apps. That entry only exists in a packaged build, so it is the one
    thing that cannot be tested by running the app from Visual Studio.
  * Tray icon, Start tile and ALT+TAB at 100% / 150% / 200% display scaling.
  * Settings survive a restart of the app.

Uninstall with:  .\test-package.ps1 -Uninstall
"@ -ForegroundColor Gray
