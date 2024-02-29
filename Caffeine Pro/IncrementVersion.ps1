# Read the current version from the .csproj file
$proj = "./Caffeine Pro.csproj"
[xml]$csproj = Get-Content $proj
$version = $csproj.Project.PropertyGroup.Version

# Increment the version
$versionParts = $version.Split('.')
$versionParts[2] = [int]$versionParts[2] + 1
$newVersion = $versionParts -join '.'

"Version update: " + $version + " -> " + $newVersion

# Update the .csproj file
$csproj.Project.PropertyGroup.Version = $newVersion
$csproj.Save($proj)