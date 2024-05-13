# Get the ID and security principal of the current user account
$myWindowsID=[System.Security.Principal.WindowsIdentity]::GetCurrent()
$myWindowsPrincipal=new-object System.Security.Principal.WindowsPrincipal($myWindowsID)

# Get the security principal for the Administrator role
$adminRole=[System.Security.Principal.WindowsBuiltInRole]::Administrator

# Check to see if we are currently running "as Administrator"
if ($myWindowsPrincipal.IsInRole($adminRole))
{
   # We are running "as Administrator" - so change the title and background color to indicate this
   $Host.UI.RawUI.WindowTitle = $myInvocation.MyCommand.Definition + "(Elevated)"
   $Host.UI.RawUI.BackgroundColor = "DarkBlue"
   clear-host
}
else
{
   Write-Host "Should run as Administrator"

   # Exit from the current, unelevated, process
   exit
}

# Run your code that needs to be elevated here
Write-Host "Running as an Administrator."

Write-Host "Signing file: E:\dev\Caffein Pro\Caffeine Pro\bin\publish-release\Caffeine Pro.exe"

signtool.exe sign /fd sha256 /tr http://ts.ssl.com /td sha256 /n "Lotrasoft Inc." "E:\dev\Caffein Pro\Caffeine Pro\bin\publish-release\Caffeine Pro.exe"