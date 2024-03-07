
# This PowerShell script runs before every build using the BeforeBuild target 
# to update the version number in the .csproj file
#
# The version number is in the format "major.minor.patch" and the patch number is incremented by 1

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

#Update the setup .aip file
$setupfile = "../Caffeine Pro Setup/Caffeine Pro Setup.aip"
[xml]$setup = Get-Content $setupfile
$node = $setup.SelectSingleNode("//ROW[@Property='ProductVersion']/@Value")
$node.Value = $newVersion

# Following line only applies to UWP .aip setup
$node = $setup.SelectSingleNode("//ROW[@Name='Version' and @XmlAttribute='Version']/@Value")
if ($node -ne $null)
{
	$node.Value = $newVersion
}
$setup.Save($setupfile)
