@echo Publishing Caffeine Pro standalone executable
@echo.

rem Paths below are relative to the repository root, so move there regardless of where the caller
rem ran this from. setlocal makes cmd restore the original directory when the script ends.
setlocal
cd /d "%~dp0.."

dotnet publish CaffeinePro\CaffeinePro.csproj -p:PublishProfile=CaffeinePro\Properties\PublishProfiles\x86.pubxml
signtool.exe sign /fd sha256 /tr http://ts.ssl.com /td sha256 /n "Lotrasoft Inc." "CaffeinePro\bin\publish\win-x86\*.exe"

@echo.
@echo Output: CaffeinePro\bin\publish\win-x86
@echo Self-contained single file - it runs on a machine with no .NET installed.
@echo For the Microsoft Store package, run publish-store.bat instead.
