<#
.SYNOPSIS
    Creates a Microsoft Store submission for Caffeine Pro and uploads the .msixupload package.

.DESCRIPTION
    Drives the Microsoft Store submission API:

      1. Gets a Microsoft Entra token using the client-credentials flow.
      2. Deletes any submission left pending on the product (optional, see -DeletePending).
      3. Creates a submission, which clones the last published one - so the Store listing, age
         ratings and declarations carry over untouched.
      4. Zips the package and block-uploads it to the Azure Blob SAS URL the API hands back.
      5. Marks the previous packages for deletion, registers the new one, and saves the submission.
      6. Commits it, but only when you pass -Commit, then polls until certification reports back.

    Without -Commit the submission is left in PendingCommit so you can review it in Partner Center
    before anything goes to certification. Run the script again with -Commit to release it.

    IMPORTANT: once a submission is created through the API, do not *edit* it in Partner Center.
    Microsoft's documentation is explicit that doing so makes the submission uncommittable through
    the API, and it can end up stuck in an error state that requires deleting and starting over.
    Viewing it is fine; changing it is not.

.PARAMETER ApplicationId
    The Store ID of the product. Defaults to Caffeine Pro's.

.PARAMETER PackagePath
    The .msixupload to submit. Defaults to the output of publish-store.bat.

.PARAMETER Commit
    Commit the submission, sending it to certification. Omit for a dry run that stops short.

.PARAMETER DeletePending
    Delete an existing pending submission instead of aborting. Destructive: whatever was in that
    pending submission is lost.

.EXAMPLE
    .\submit-store.ps1
    Create the submission and upload the package, stopping before certification.

.EXAMPLE
    .\submit-store.ps1 -Commit
    The same, then send it to certification and follow the status.

.PARAMETER EnvFile
    The .env file holding the Partner Center credentials. Defaults to .env beside this script.

.NOTES
    Credentials are read from scripts\.env - never from arguments, so they stay out of your shell
    history and off the process command line. Copy scripts\.env.example to scripts\.env and fill
    it in:

        PARTNER_TENANT_ID=...
        PARTNER_CLIENT_ID=...
        PARTNER_CLIENT_SECRET=...

    .env is gitignored and must stay that way; it holds a secret that can publish under your
    company's identity. If one ever does get committed, rotate it in Partner Center immediately.

    Values in .env take precedence over variables already set in the session, so editing the file
    is always what takes effect. If .env is absent the script falls back to real environment
    variables, which is what a CI runner would use.

    The Entra application must be associated with the Partner Center account and hold the Manager
    role. The product must already have one completed submission, including the age-ratings
    questionnaire - the API cannot create a product's first submission.
#>

[CmdletBinding()]
param(
    [string] $ApplicationId = '9N1K4J8VDZVV',

    # This script lives in scripts\, so the package hangs off the parent directory.
    [string] $PackagePath = (Join-Path (Split-Path $PSScriptRoot -Parent) 'CaffeinePro Setup\AppPackages\CaffeinePro Setup_3.0.0.0_x86_x64_arm64_bundle.msixupload'),

    # Sits next to this script, alongside the .env.example it is copied from.
    [string] $EnvFile = (Join-Path $PSScriptRoot '.env'),

    [switch] $Commit,
    [switch] $DeletePending,

    # Authenticate and read the product, then stop. Creates nothing, so it is safe to run
    # repeatedly while getting the credentials right.
    [switch] $CheckOnly
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$ApiRoot   = 'https://manage.devcenter.microsoft.com/v1.0/my'
$Resource  = 'https://manage.devcenter.microsoft.com'
$BlockSize = 4MB   # Safe for every Azure Blob service version a SAS might pin.

function Write-Step { param([string] $Text) Write-Host "`n==> $Text" -ForegroundColor Cyan }
function Write-Note { param([string] $Text) Write-Host "    $Text" -ForegroundColor DarkGray }

# StrictMode turns a missing property into a terminating error, and the shape of these API
# responses varies with the state of the product, so optional fields are read through this.
function Get-Prop {
    param($Object, [string] $Name, $Default = $null)
    if ($null -eq $Object) { return $Default }
    $prop = $Object.PSObject.Properties[$Name]
    if ($null -eq $prop -or $null -eq $prop.Value) { return $Default }
    return $prop.Value
}

# Minimal .env reader: KEY=VALUE, one per line. Handles blank lines, # comments, an optional
# "export " prefix, surrounding quotes, and values that themselves contain "=". Unquoted values
# have a trailing " # comment" stripped, so quote anything that legitimately contains one.
function Import-DotEnv {
    param([string] $Path)

    if (-not (Test-Path -LiteralPath $Path)) { return 0 }

    # A secret in a tracked file is a much bigger problem than a failed submission.
    $tracked = & git -C (Split-Path $Path -Parent) ls-files --error-unmatch -- $Path 2>$null
    if ($LASTEXITCODE -eq 0 -and $tracked) {
        throw "'$Path' is tracked by git. It holds a secret and must not be. Run " +
              "'git rm --cached `"$Path`"', confirm .gitignore covers it, and rotate the secret " +
              "in Partner Center."
    }

    $loaded = 0
    foreach ($line in Get-Content -LiteralPath $Path) {
        $text = $line.Trim()
        if ($text -eq '' -or $text.StartsWith('#')) { continue }
        if ($text -notmatch '^(?:export\s+)?([A-Za-z_][A-Za-z0-9_]*)\s*=\s*(.*)$') { continue }

        $name  = $Matches[1]
        $value = $Matches[2].Trim()

        $quoted = $value.Length -ge 2 -and
                  (($value[0] -eq '"' -and $value[-1] -eq '"') -or
                   ($value[0] -eq "'" -and $value[-1] -eq "'"))
        if ($quoted) {
            $value = $value.Substring(1, $value.Length - 2)
        }
        elseif ($value -match '^(.*?)\s+#.*$') {
            $value = $Matches[1].TrimEnd()
        }

        Set-Item -LiteralPath "Env:$name" -Value $value
        $loaded++
    }
    return $loaded
}

function Get-RequiredEnv {
    param([string] $Name)
    $value = [Environment]::GetEnvironmentVariable($Name)
    if ([string]::IsNullOrWhiteSpace($value)) {
        throw "Environment variable $Name is not set. See the notes in this script's header."
    }
    return $value
}

function Get-AccessToken {
    $body = @{
        grant_type    = 'client_credentials'
        client_id     = Get-RequiredEnv 'PARTNER_CLIENT_ID'
        client_secret = Get-RequiredEnv 'PARTNER_CLIENT_SECRET'
        resource      = $Resource
    }
    $tenant = Get-RequiredEnv 'PARTNER_TENANT_ID'
    $uri = "https://login.microsoftonline.com/$tenant/oauth2/token"

    try {
        return (Invoke-RestMethod -Method Post -Uri $uri -Body $body).access_token
    }
    catch {
        throw "Could not get an access token. Check the tenant, client id and secret, and that " +
              "the application is associated with the Partner Center account. $($_.Exception.Message)"
    }
}

function Invoke-StoreApi {
    param(
        [string] $Method,
        [string] $Path,
        $Body
    )
    $params = @{
        Method  = $Method
        Uri     = "$ApiRoot/$Path"
        Headers = @{ Authorization = "Bearer $script:Token" }
    }
    if ($null -ne $Body) {
        $params.Body        = ($Body | ConvertTo-Json -Depth 40 -Compress)
        $params.ContentType = 'application/json'
    }

    try { return Invoke-RestMethod @params }
    catch {
        $detail = ''
        if ($_.ErrorDetails -and $_.ErrorDetails.Message) { $detail = " - $($_.ErrorDetails.Message)" }
        throw "$Method $Path failed: $($_.Exception.Message)$detail"
    }
}

# Uploads in blocks rather than one PUT. A single Put Blob is capped by the service version pinned
# in the SAS token - as low as 64 MB - and the package is far larger than that.
function Send-PackageToBlob {
    param(
        [string] $ZipPath,
        [string] $SasUrl
    )
    $stream = [System.IO.File]::OpenRead($ZipPath)
    try {
        $total     = $stream.Length
        $blockIds  = New-Object System.Collections.Generic.List[string]
        $buffer    = New-Object byte[] $BlockSize
        $index     = 0
        $sent      = 0

        while (($read = $stream.Read($buffer, 0, $buffer.Length)) -gt 0) {
            $blockId = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($index.ToString('D6')))
            $blockIds.Add($blockId)

            $chunk = New-Object byte[] $read
            [Array]::Copy($buffer, $chunk, $read)

            $uri = "$SasUrl&comp=block&blockid=$([Uri]::EscapeDataString($blockId))"
            Invoke-RestMethod -Method Put -Uri $uri -Body $chunk `
                              -Headers @{ 'x-ms-blob-type' = 'BlockBlob' } | Out-Null

            $sent += $read
            $index++
            Write-Progress -Activity 'Uploading package' -Status "$([math]::Round($sent/1MB)) MB of $([math]::Round($total/1MB)) MB" `
                           -PercentComplete (($sent / $total) * 100)
        }
        Write-Progress -Activity 'Uploading package' -Completed

        $xml = "<?xml version='1.0' encoding='utf-8'?><BlockList>" +
               (($blockIds | ForEach-Object { "<Latest>$_</Latest>" }) -join '') +
               "</BlockList>"
        Invoke-RestMethod -Method Put -Uri "$SasUrl&comp=blocklist" -Body $xml `
                          -ContentType 'application/xml' | Out-Null

        Write-Note "Uploaded $index block(s), $([math]::Round($total/1MB,1)) MB"
    }
    finally { $stream.Dispose() }
}

# ---------------------------------------------------------------------------------------------

if (-not (Test-Path $PackagePath)) {
    throw "Package not found at '$PackagePath'. Run publish-store.bat first."
}
$package = Get-Item $PackagePath

$loaded = Import-DotEnv -Path $EnvFile
if ($loaded -gt 0) {
    Write-Note "Loaded $loaded value(s) from $EnvFile"
}
elseif (-not (Test-Path -LiteralPath $EnvFile)) {
    Write-Note "No $EnvFile - falling back to the session's environment variables"
}

Write-Step 'Authenticating'
$script:Token = Get-AccessToken
Write-Note 'Token acquired'

Write-Step "Reading product $ApplicationId"
$app = Invoke-StoreApi -Method Get -Path "applications/$ApplicationId"
Write-Note "Name: $(Get-Prop $app 'primaryName' '<unknown>')"

$pending = Get-Prop $app 'pendingApplicationSubmission'

if ($CheckOnly) {
    $lastPublished = Get-Prop $app 'lastPublishedApplicationSubmission'
    Write-Host "`nCredentials work." -ForegroundColor Green
    Write-Note "Package to submit : $($package.Name)"
    Write-Note "Pending submission: $(if ($pending) { Get-Prop $pending 'id' } else { 'none' })"
    if ($lastPublished) {
        Write-Note "Last published    : $(Get-Prop $lastPublished 'id')"
    }
    else {
        Write-Host "`nNo published submission found. The API cannot create a product's first" -ForegroundColor Yellow
        Write-Host "submission - do that once in Partner Center, including the age-ratings" -ForegroundColor Yellow
        Write-Host "questionnaire, before using this script." -ForegroundColor Yellow
    }
    return
}

if ($pending) {
    $pendingId = Get-Prop $pending 'id' ''

    if (-not $DeletePending) {
        throw "Submission $pendingId is already pending for this product. Finish or remove it in " +
              "Partner Center, or re-run with -DeletePending to discard it."
    }
    Write-Step "Deleting pending submission $pendingId"
    Invoke-StoreApi -Method Delete -Path "applications/$ApplicationId/submissions/$pendingId" | Out-Null
}

Write-Step 'Creating submission'
$submission   = Invoke-StoreApi -Method Post -Path "applications/$ApplicationId/submissions"
$submissionId = $submission.id
Write-Note "Submission $submissionId (clones the last published submission)"

Write-Step 'Packing'
$zipPath = Join-Path ([IO.Path]::GetTempPath()) "CaffeinePro-$submissionId.zip"
if (Test-Path $zipPath) { Remove-Item $zipPath -Force }

Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [System.IO.Compression.ZipFile]::Open($zipPath, 'Create')
try {
    # NoCompression: an .msixupload is already a compressed archive, so re-deflating it costs
    # minutes and saves nothing.
    [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile(
        $zip, $package.FullName, $package.Name, [System.IO.Compression.CompressionLevel]::NoCompression) | Out-Null
}
finally { $zip.Dispose() }
Write-Note "$($package.Name) -> $([math]::Round((Get-Item $zipPath).Length/1MB,1)) MB zip"

Write-Step 'Uploading'
Send-PackageToBlob -ZipPath $zipPath -SasUrl $submission.fileUploadUrl

Write-Step 'Registering the package on the submission'
# Retire whatever the cloned submission carried over, then add this build.
$existingPackages = @(Get-Prop $submission 'applicationPackages' @())
foreach ($existing in $existingPackages) {
    $existing.fileStatus = 'PendingDelete'
}
$submission.applicationPackages = $existingPackages + [pscustomobject]@{
    fileName             = $package.Name
    fileStatus           = 'PendingUpload'
    minimumDirectXVersion = 'None'
    minimumSystemRam      = 'None'
}
Invoke-StoreApi -Method Put -Path "applications/$ApplicationId/submissions/$submissionId" -Body $submission | Out-Null
Write-Note "Retired $($existingPackages.Count) previous package entry/entries, added $($package.Name)"

Remove-Item $zipPath -Force -ErrorAction SilentlyContinue

if (-not $Commit) {
    Write-Host @"

Submission $submissionId is staged but NOT committed.

Review it read-only at:
  https://partner.microsoft.com/dashboard/products/$ApplicationId/submissions/$submissionId

Do not edit it in Partner Center - that would make it uncommittable through this script. When it
looks right, send it to certification with:

  .\submit-store.ps1 -Commit

"@ -ForegroundColor Yellow
    return
}

Write-Step 'Committing to certification'
Invoke-StoreApi -Method Post -Path "applications/$ApplicationId/submissions/$submissionId/commit" | Out-Null

Write-Step 'Waiting for ingestion'
do {
    Start-Sleep -Seconds 20
    $status = Invoke-StoreApi -Method Get -Path "applications/$ApplicationId/submissions/$submissionId/status"
    Write-Note (Get-Prop $status 'status' '<no status>')
} while ((Get-Prop $status 'status') -in @('CommitStarted', 'PreProcessing'))

$details  = Get-Prop $status 'statusDetails'
$errors   = @(Get-Prop $details 'errors'   @())
$warnings = @(Get-Prop $details 'warnings' @())

if ($status.status -like '*Failed*') {
    $errors | ForEach-Object { Write-Host "  ERROR: $(Get-Prop $_ 'code') $(Get-Prop $_ 'details')" -ForegroundColor Red }
    throw "Submission failed with status '$($status.status)'."
}

$warnings | ForEach-Object { Write-Host "  warning: $(Get-Prop $_ 'code') $(Get-Prop $_ 'details')" -ForegroundColor Yellow }

Write-Host "`nSubmitted. Status: $($status.status)" -ForegroundColor Green
Write-Host "Certification usually takes a few hours; track it at" -ForegroundColor Gray
Write-Host "  https://partner.microsoft.com/dashboard/products/$ApplicationId/submissions/$submissionId" -ForegroundColor Gray
