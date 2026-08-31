<#
.SYNOPSIS
    Checks that every translation file is in lockstep with the neutral (English) one.

.DESCRIPTION
    The UI reads its text from CaffeinePro\Resources\Strings.resx and one satellite .resx per
    language. Nothing at build time complains when a key is added to the neutral file and forgotten
    in the others - the app simply falls back to English at run time, which is easy to miss. This
    script makes that visible, and also catches the mistake that a fallback would not save you from:
    a translated composite format that lost or renamed one of its {0} placeholders, which throws a
    FormatException the moment that string is shown.

    Run it after touching any .resx. Exits non-zero if anything is wrong, so it can be wired into a
    build or a pre-commit hook.

.EXAMPLE
    pwsh Scripts\check-localization.ps1
#>

[CmdletBinding()]
param(
    [string] $ResourcesPath = (Join-Path $PSScriptRoot '..\CaffeinePro\Resources')
)

$ErrorActionPreference = 'Stop'

function Get-ResxEntries([string] $Path) {
    $entries = [ordered]@{}
    foreach ($node in ([xml](Get-Content -Raw -Path $Path)).root.data) {
        $entries[$node.name] = $node.value
    }
    return $entries
}

function Get-Placeholders([string] $Text) {
    return ([regex]::Matches($Text, '\{(\d+)')  | ForEach-Object { $_.Groups[1].Value } |
        Sort-Object -Unique) -join ','
}

$neutralPath = Join-Path $ResourcesPath 'Strings.resx'
if (-not (Test-Path $neutralPath)) {
    throw "Neutral resource file not found: $neutralPath"
}

$neutral = Get-ResxEntries $neutralPath
Write-Output ("Strings.resx: {0} keys" -f $neutral.Count)

$problems = 0

foreach ($file in Get-ChildItem -Path $ResourcesPath -Filter 'Strings.*.resx' | Sort-Object Name) {
    $language = $file.BaseName -replace '^Strings\.', ''
    $translated = Get-ResxEntries $file.FullName

    $missing = @($neutral.Keys | Where-Object { -not $translated.Contains($_) })
    $extra = @($translated.Keys | Where-Object { -not $neutral.Contains($_) })
    $badFormat = @()

    foreach ($key in $neutral.Keys) {
        if (-not $translated.Contains($key)) { continue }
        if ((Get-Placeholders $neutral[$key]) -ne (Get-Placeholders $translated[$key])) {
            $badFormat += $key
        }
    }

    $count = $missing.Count + $extra.Count + $badFormat.Count
    $problems += $count

    if ($count -eq 0) {
        Write-Output ("  {0,-8} OK ({1} keys)" -f $language, $translated.Count)
        continue
    }

    Write-Output ("  {0,-8} {1} problem(s)" -f $language, $count)
    foreach ($key in $missing) { Write-Output "      missing:     $key" }
    foreach ($key in $extra) { Write-Output "      not in en:   $key" }
    foreach ($key in $badFormat) {
        Write-Output ("      placeholders differ: {0}" -f $key)
        Write-Output ("          en: {0}" -f $neutral[$key])
        Write-Output ("          {0}: {1}" -f $language, $translated[$key])
    }
}

if ($problems -gt 0) {
    Write-Output ''
    Write-Output "$problems localization problem(s) found."
    exit 1
}

Write-Output ''
Write-Output 'All translations are complete and consistent.'
