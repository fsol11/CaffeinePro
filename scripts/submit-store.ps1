<#
.SYNOPSIS
    Creates a Microsoft Store submission for Caffeine Pro and uploads the .msixupload package.

.DESCRIPTION
    Drives the Microsoft Store submission API:

      1. Gets a Microsoft Entra token using the client-credentials flow.
      2. Deletes any submission left pending on the product (optional, see -DeletePending).
      3. Creates a submission, which clones the last published one - so the Store listing, age
         ratings and declarations carry over untouched.
      4. Replaces every language's listing text, desktop screenshots and captions from the
         listing CSV (skip with -SkipListing).
      5. Zips the package and the screenshots and block-uploads them to the Azure Blob SAS URL
         the API hands back.
      6. Marks the previous packages for deletion, registers the new one, and saves the submission.
      7. Commits it, but only when you pass -Commit, then polls until certification reports back.

    Without -Commit the submission is left in PendingCommit so you can review it in Partner Center
    before anything goes to certification. Run the script again with -Commit to release it.

    IMPORTANT: once a submission is created through the API, do not *edit* it in Partner Center.
    Microsoft's documentation is explicit that doing so makes the submission uncommittable through
    the API, and it can end up stuck in an error state that requires deleting and starting over.
    Viewing it is fine; changing it is not.

.PARAMETER ApplicationId
    The Store ID of the product. Defaults to Caffeine Pro's.

.PARAMETER PackagePath
    The .msixupload to submit, or a folder to look in. Defaults to whatever version publish-store.bat
    last built into CaffeinePro Setup\AppPackages - the script reads the version off the package it
    finds, so there is nothing to bump here between releases.

.PARAMETER ListingCsv
    The Partner Center listing export that holds each language's description, release notes,
    features and search terms, and says which screenshots it gets, in which order, with which
    captions. Defaults to Store\import\listings.csv. Screenshot paths in it are
    relative to the folder holding import\, i.e. Store\ - the same paths Partner Center's manual
    "Import listings" flow resolves, so one file drives both routes.

.PARAMETER SkipListing
    Leave the cloned listing exactly as published - text and screenshots both - and submit only the
    package.

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

    # Empty means "find the build". This script lives in scripts\, so the packages hang off the
    # parent directory; see Resolve-PackagePath.
    [string] $PackagePath,

    # Sits next to this script, alongside the .env.example it is copied from.
    [string] $EnvFile = (Join-Path $PSScriptRoot '.env'),

    [string] $ListingCsv = (Join-Path (Split-Path $PSScriptRoot -Parent) 'Store\import\listings.csv'),

    [switch] $SkipListing,
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

$AppPackages = Join-Path (Split-Path $PSScriptRoot -Parent) 'CaffeinePro Setup\AppPackages'

function Write-Step { param([string] $Text) Write-Host "`n==> $Text" -ForegroundColor Cyan }
function Write-Note { param([string] $Text) Write-Host "    $Text" -ForegroundColor DarkGray }

# Finds the .msixupload to submit instead of naming a version, so a rebuild at any version is
# submittable without editing this script. $Path may be a file (used as-is), a folder to search,
# or empty to search the standard AppPackages output.
function Resolve-PackagePath {
    param([string] $Path)

    if ($Path -and (Test-Path -LiteralPath $Path -PathType Leaf)) {
        return (Get-Item -LiteralPath $Path)
    }

    $searchRoot = if ($Path) { $Path } else { $AppPackages }
    if (-not (Test-Path -LiteralPath $searchRoot)) {
        throw "No package and no '$searchRoot' to look in. Run publish-store.bat first, or pass " +
              "-PackagePath."
    }

    # Recursive: the .msixupload sits directly in AppPackages, but a folder handed in through
    # -PackagePath may well be a parent of it.
    $found = @(Get-ChildItem -LiteralPath $searchRoot -Filter *.msixupload -File -Recurse |
               Sort-Object LastWriteTime -Descending)

    if ($found.Count -eq 0) {
        throw "No .msixupload under '$searchRoot'. Run publish-store.bat first."
    }

    # publish-store.bat wipes AppPackages before building precisely so there is only ever one
    # candidate. Two means something else left a package behind - a solution build drops a
    # single-architecture one - and guessing here is how the wrong build reaches the Store.
    if ($found.Count -gt 1) {
        $list = ($found | ForEach-Object { "  $($_.Name)  ($($_.LastWriteTime))" }) -join "`n"
        throw "Found $($found.Count) .msixupload files under '$searchRoot' and will not guess:`n" +
              "$list`nRe-run publish-store.bat for a clean build, or pass -PackagePath explicitly."
    }

    return $found[0]
}

# The version is in the file name the packaging project generates, e.g.
# "CaffeinePro Setup_3.1.0.0_x86_x64_arm64_bundle.msixupload". Only used for display.
function Get-PackageVersion {
    param([string] $FileName)
    if ($FileName -match '_(\d+(?:\.\d+){1,3})_') { return $Matches[1] }
    return '<unknown>'
}

# Reads the listing CSV into "language -> ordered screenshots". Driving this off the same file
# Partner Center's manual import reads means the listing has one definition, not one per route:
# reorder a screenshot or reword a caption there and both this script and a manual import follow.
function Import-ListingScreenshots {
    param([string] $CsvPath)

    if (-not (Test-Path -LiteralPath $CsvPath -PathType Leaf)) {
        throw "No listing CSV at '$CsvPath'. Pass -ListingCsv, or -SkipListing to submit the " +
              "package without touching the listing."
    }

    $rows = @(Import-Csv -LiteralPath $CsvPath -Encoding UTF8)
    if ($rows.Count -eq 0) { throw "'$CsvPath' has no rows." }

    $byField = @{}
    foreach ($row in $rows) { $byField[$row.Field] = $row }

    # The export's fixed leading columns; everything after them is a listing language.
    $fixed     = @('Field', 'ID', 'Type (Type)', 'default')
    $languages = @($rows[0].PSObject.Properties.Name | Where-Object { $_ -notin $fixed })
    if ($languages.Count -eq 0) { throw "'$CsvPath' has no language columns." }

    # Paths read "import/en-light-menu.png" and resolve against the folder that holds import\,
    # which is the CSV's grandparent: Store\import\listings.csv -> Store\.
    $assetRoot = Split-Path (Split-Path $CsvPath -Parent) -Parent

    $result = @{}
    foreach ($language in $languages) {
        $shots = New-Object System.Collections.Generic.List[object]

        # Partner Center offers 30 desktop slots; only the filled ones are part of the listing.
        for ($slot = 1; $slot -le 30; $slot++) {
            $pathRow = $byField["DesktopScreenshot$slot"]
            if ($null -eq $pathRow) { continue }

            $relative = ([string] (Get-Prop $pathRow $language '')).Trim()
            if ($relative -eq '') { continue }

            # A path that survived from an earlier export still points at Partner Center rather
            # than at a file here, and would upload nothing.
            if ($relative -match '^https?://') {
                throw "$language DesktopScreenshot$slot is still a Partner Center URL, not a file: " +
                      "$relative. Re-point it at a screenshot under '$assetRoot'."
            }

            $full = Join-Path $assetRoot $relative
            if (-not (Test-Path -LiteralPath $full -PathType Leaf)) {
                throw "$language DesktopScreenshot$slot points at '$relative', which does not exist " +
                      "under '$assetRoot'."
            }

            $captionRow = $byField["DesktopScreenshotCaption$slot"]
            $caption    = if ($null -ne $captionRow) { ([string] (Get-Prop $captionRow $language '')).Trim() } else { '' }

            $shots.Add([pscustomobject]@{
                Slot    = $slot
                File    = (Get-Item -LiteralPath $full)
                Caption = $caption
            })
        }

        if ($shots.Count -gt 0) { $result[$language] = $shots }
    }

    if ($result.Count -eq 0) { throw "'$CsvPath' lists no desktop screenshots for any language." }
    return $result
}

# CSV heading -> the property of the same text on the API's base listing resource. The two
# vocabularies disagree often enough (CopyrightTrademarkInformation against copyrightAndTrademarkInfo)
# that guessing at the call site is how a field silently stops being written.
$ListingTextFields = [ordered]@{
    'Description'                   = 'description'
    'ReleaseNotes'                  = 'releaseNotes'
    'Title'                         = 'title'
    'ShortTitle'                    = 'shortTitle'
    'SortTitle'                     = 'sortTitle'
    'VoiceTitle'                    = 'voiceTitle'
    'ShortDescription'              = 'shortDescription'
    'DevStudio'                     = 'devStudio'
    'CopyrightTrademarkInformation' = 'copyrightAndTrademarkInfo'
    'AdditionalLicenseTerms'        = 'licenseTerms'
}

# The same, for the fields the export spreads over numbered columns and the API takes as an array.
# Counts are Partner Center's caps for each.
$ListingArrayFields = [ordered]@{
    'Feature'               = @{ Property = 'features';            Max = 20 }
    'SearchTerm'            = @{ Property = 'keywords';             Max = 7  }
    'MinimumHardwareReq'    = @{ Property = 'minimumHardware';      Max = 11 }
    'RecommendedHardwareReq'= @{ Property = 'recommendedHardware';  Max = 11 }
}

# Reads the listing's text out of the same CSV, so description, release notes and the rest travel
# with the screenshots instead of silently keeping whatever the cloned submission carried over.
function Import-ListingText {
    param([string] $CsvPath)

    $rows    = @(Import-Csv -LiteralPath $CsvPath -Encoding UTF8)
    $byField = @{}
    foreach ($row in $rows) { $byField[$row.Field] = $row }

    $fixed     = @('Field', 'ID', 'Type (Type)', 'default')
    $languages = @($rows[0].PSObject.Properties.Name | Where-Object { $_ -notin $fixed })

    $result = @{}
    foreach ($language in $languages) {
        $values = [ordered]@{}

        foreach ($heading in $ListingTextFields.Keys) {
            $row = $byField[$heading]
            if ($null -eq $row) { continue }
            $values[$ListingTextFields[$heading]] = ([string] (Get-Prop $row $language '')).Trim()
        }

        foreach ($prefix in $ListingArrayFields.Keys) {
            $spec  = $ListingArrayFields[$prefix]
            $items = New-Object System.Collections.Generic.List[string]
            for ($i = 1; $i -le $spec.Max; $i++) {
                $row = $byField["$prefix$i"]
                if ($null -eq $row) { continue }
                $value = ([string] (Get-Prop $row $language '')).Trim()
                if ($value -ne '') { $items.Add($value) }
            }
            $values[$spec.Property] = $items.ToArray()
        }

        $result[$language] = $values
    }
    return $result
}

# The submission's listing keys and the CSV's column headings line up in practice, but not always
# exactly: casing can differ ("zh-Hans" against "zh-hans"), and a regional listing is content to be
# served by its base language's column (pt-br from pt). PowerShell's -eq is case-insensitive, which
# is what the first pass wants.
function Resolve-LanguageColumn {
    param([string] $Language, [hashtable] $Screenshots)

    foreach ($column in $Screenshots.Keys) {
        if ($column -eq $Language) { return $column }
    }
    $base = $Language.Split('-')[0]
    foreach ($column in $Screenshots.Keys) {
        if ($column -eq $base) { return $column }
    }
    return $null
}

# Writes a value onto a listing whether or not the API returned that property. StrictMode makes
# assigning to an absent property a terminating error, and which optional fields come back depends
# on what the listing happens to have set.
function Set-ListingProperty {
    param($Target, [string] $Name, $Value)

    if ($Target.PSObject.Properties[$Name]) { $Target.$Name = $Value }
    else { $Target | Add-Member -NotePropertyName $Name -NotePropertyValue $Value }
}

# Applies the CSV's text to every listing and reports what actually changed, so a run that was
# meant to touch only the release notes is visibly a run that touched only the release notes.
function Set-ListingText {
    param($Submission, [hashtable] $Text)

    $listings = Get-Prop $Submission 'listings'
    $changed  = 0

    foreach ($property in $listings.PSObject.Properties) {
        $language    = $property.Name
        $baseListing = Get-Prop $property.Value 'baseListing'
        if ($null -eq $baseListing) { continue }

        $column = Resolve-LanguageColumn -Language $language -Screenshots $Text
        if ($null -eq $column) { continue }

        foreach ($name in $Text[$column].Keys) {
            $new = $Text[$column][$name]
            $old = Get-Prop $baseListing $name

            # Arrays compare by content; a joined string is enough to spot a real difference.
            if ($new -is [array]) {
                $newKey = ($new -join "`u{1}")
                $oldKey = (@($old) -join "`u{1}")
            }
            else {
                $newKey = [string] $new
                $oldKey = [string] $old
            }

            if ($newKey -eq $oldKey) { continue }

            Set-ListingProperty -Target $baseListing -Name $name -Value $new
            $changed++

            $shown = if ($new -is [array]) { "$($new.Count) item(s)" }
                     else { $newKey -replace "`r?`n", ' / ' }
            if ($shown.Length -gt 60) { $shown = $shown.Substring(0, 60) + '...' }
            Write-Note ("{0,-10} {1} <- {2}" -f $language, $name, $shown)
        }
    }

    if ($changed -eq 0) { Write-Note 'No text differs from the cloned submission.' }
}

# Points every listing at the screenshots from the CSV and reports the zip entries that have to
# accompany them. Returns the entries; mutates $Submission in place.
function Set-ListingScreenshots {
    param($Submission, [hashtable] $Screenshots)

    $entries   = New-Object System.Collections.Generic.List[object]
    $unmatched = New-Object System.Collections.Generic.List[string]
    $listings  = Get-Prop $Submission 'listings'

    if ($null -eq $listings) {
        throw "The submission has no listings to update. Check the product in Partner Center."
    }

    foreach ($property in $listings.PSObject.Properties) {
        $language    = $property.Name
        $baseListing = Get-Prop $property.Value 'baseListing'
        if ($null -eq $baseListing) { continue }

        $column = Resolve-LanguageColumn -Language $language -Screenshots $Screenshots
        if ($null -eq $column) { $unmatched.Add($language); continue }

        # Retire the desktop screenshots already on the listing and leave every other image alone:
        # the Store logos and promotional art share this array and are not being changed here.
        $images  = New-Object System.Collections.Generic.List[object]
        $retired = 0
        foreach ($image in @(Get-Prop $baseListing 'images' @())) {
            if ((Get-Prop $image 'imageType' '') -eq 'Screenshot') {
                $image.fileStatus = 'PendingDelete'
                $retired++
            }
            $images.Add($image)
        }

        # Namespaced by language so that a file shared between listings - pt-br reuses pt's
        # screenshots - gets one zip entry per listing rather than one shared entry.
        foreach ($shot in $Screenshots[$column]) {
            $entryPath = "images/$language/$($shot.File.Name)"
            $entries.Add([pscustomobject]@{ Source = $shot.File; EntryPath = $entryPath })
            $images.Add([pscustomobject]@{
                fileName    = $entryPath
                fileStatus  = 'PendingUpload'
                imageType   = 'Screenshot'
                description = $shot.Caption
            })
        }

        Set-ListingProperty -Target $baseListing -Name 'images' -Value $images.ToArray()

        $source = if ($column -eq $language) { '' } else { " (from '$column')" }
        Write-Note ("{0,-10} {1} screenshot(s) in, {2} retired{3}" -f $language, $Screenshots[$column].Count, $retired, $source)
    }

    # Not fatal: a listing language the CSV does not cover keeps the screenshots it already has,
    # which is a coherent outcome - but it is never what you meant, so say so loudly.
    foreach ($language in $unmatched) {
        Write-Host "    WARNING: listing '$language' has no column in the CSV; its screenshots are unchanged." -ForegroundColor Yellow
    }

    return $entries
}

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
    # The API rejects a request whose media type is not JSON, and that includes the bodyless POSTs
    # that create and commit a submission: with no body at all, Invoke-RestMethod sends no
    # Content-Type and the call comes back "Only JSON content is accepted". So those get empty
    # content carrying the JSON type, which is what Microsoft's own client sends.
    #
    # Empty content, not "{}": an empty object is parsed as a submission and rejected for having
    # no listings. These endpoints take everything from the URL; the body has to say nothing.
    if ($null -ne $Body) {
        $params.Body        = ($Body | ConvertTo-Json -Depth 40 -Compress)
        $params.ContentType = 'application/json'
    }
    elseif ($Method -in @('Post', 'Put', 'Patch')) {
        $params.Body        = ''
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

$package = Resolve-PackagePath -Path $PackagePath
Write-Note "Package: $($package.Name) (version $(Get-PackageVersion $package.Name))"

# Read and validate the listing before authenticating: a typo in the CSV or a missing screenshot
# should fail here, not half-way through a submission that then has to be deleted.
$screenshots = $null
$listingText = $null
if ($SkipListing) {
    Write-Note 'Listing: skipped (-SkipListing); the cloned submission keeps its own'
}
else {
    $screenshots = Import-ListingScreenshots -CsvPath $ListingCsv
    $listingText = Import-ListingText        -CsvPath $ListingCsv
    $shotCount   = ($screenshots.Values | ForEach-Object { $_.Count } | Measure-Object -Sum).Sum
    Write-Note "Listing: $shotCount screenshot(s) across $($screenshots.Count) language(s) from $ListingCsv"
}

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
    Write-Note "Package to submit : $($package.Name) (version $(Get-PackageVersion $package.Name))"
    if ($null -ne $screenshots) {
        foreach ($language in ($screenshots.Keys | Sort-Object)) {
            $names = ($screenshots[$language] | ForEach-Object { $_.File.Name }) -join ', '
            Write-Note "  $language : $names"
        }
    }
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
    try {
        Invoke-StoreApi -Method Delete -Path "applications/$ApplicationId/submissions/$pendingId" | Out-Null
    }
    catch {
        # The API owns only what it created. A submission started in Partner Center is refused
        # here, and the raw error does not say where to go, so spell it out.
        if ($_.Exception.Message -match 'created through the API') {
            throw "Submission $pendingId was created in Partner Center, not through this script, " +
                  "so the API will not delete it. Discard it in Partner Center first:`n" +
                  "  https://partner.microsoft.com/dashboard/products/$ApplicationId/submissions/$pendingId`n" +
                  "then re-run this script (no -DeletePending needed once it is gone)."
        }
        throw
    }
}

Write-Step 'Creating submission'
$submission   = Invoke-StoreApi -Method Post -Path "applications/$ApplicationId/submissions"
$submissionId = $submission.id
Write-Note "Submission $submissionId (clones the last published submission)"

$imageEntries = @()
if ($null -ne $screenshots) {
    Write-Step 'Updating the listing text'
    Set-ListingText -Submission $submission -Text $listingText

    Write-Step 'Updating the screenshots'
    $imageEntries = @(Set-ListingScreenshots -Submission $submission -Screenshots $screenshots)
}

Write-Step 'Packing'
$zipPath = Join-Path ([IO.Path]::GetTempPath()) "CaffeinePro-$submissionId.zip"
if (Test-Path $zipPath) { Remove-Item $zipPath -Force }

Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [System.IO.Compression.ZipFile]::Open($zipPath, 'Create')
try {
    # NoCompression: an .msixupload is already a compressed archive, so re-deflating it costs
    # minutes and saves nothing. The same goes for the PNGs, which are deflated already.
    [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile(
        $zip, $package.FullName, $package.Name, [System.IO.Compression.CompressionLevel]::NoCompression) | Out-Null

    $written = @{}
    foreach ($entry in $imageEntries) {
        if ($written.ContainsKey($entry.EntryPath)) { continue }
        [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile(
            $zip, $entry.Source.FullName, $entry.EntryPath, [System.IO.Compression.CompressionLevel]::NoCompression) | Out-Null
        $written[$entry.EntryPath] = $true
    }
}
finally { $zip.Dispose() }
Write-Note "$($package.Name) + $($imageEntries.Count) image(s) -> $([math]::Round((Get-Item $zipPath).Length/1MB,1)) MB zip"

Write-Step 'Uploading'
Send-PackageToBlob -ZipPath $zipPath -SasUrl $submission.fileUploadUrl

Write-Step 'Saving the submission'
# The product is on Pricing Version 2, which this API does not speak. The GET hands back the
# placeholder tier "Base" for the price, and echoing it is rejected ("'Base' is not a valid PriceId
# for base price") - but dropping the whole pricing block is rejected too ("Pricing data was not
# provided"). So the block goes back with just that one unwritable value removed: the request still
# carries pricing data, and it asserts nothing about the price, which Partner Center owns.
$pricing = Get-Prop $submission 'pricing'
if ($null -ne $pricing -and $pricing.PSObject.Properties['priceId'] -and $pricing.priceId -eq 'Base') {
    $pricing.PSObject.Properties.Remove('priceId')
    Write-Note 'Removed the placeholder price tier (product is on Pricing Version 2)'
}

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
if ($imageEntries.Count -gt 0) {
    Write-Note "Registered $($imageEntries.Count) screenshot(s) with captions"
}

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
