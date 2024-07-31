@echo Publishing Caffeine
@echo.

dotnet publish CaffeinePro\CaffeinePro.csproj -p:PublishProfile=CaffeinePro\Properties\PublishProfiles\x86.pubxml
signtool.exe sign /fd sha256 /tr http://ts.ssl.com /td sha256 /n "Lotrasoft Inc." "CaffeinePro\bin\publish\win-x86\*.exe"

 advancedinstaller /build ".\CaffeinePro AdvInstaller MSI Setup\CaffeinePro Setup.aip"
 signtool.exe sign /fd sha256 /tr http://ts.ssl.com /td sha256 /n "Lotrasoft Inc." "CaffeinePro AdvInstaller MSI Setup\CaffeinePro Setup-SetupFiles\*.msi"

