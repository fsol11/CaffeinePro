@echo Publishing Caffeine
@echo.

dotnet publish CaffeinePro\CaffeinePro.csproj -p:PublishProfile=x86
signtool.exe sign /fd sha256 /tr http://ts.ssl.com /td sha256 /n "Lotrasoft Inc." CaffeinePro\bin\publish\win-x86\CaffeinePro.exe


advinst /build "CaffeinePro AdvInstaller Setup\CaffeinePro Setup.aip"
signtool.exe sign /fd sha256 /tr http://ts.ssl.com /td sha256 /n "Lotrasoft Inc." "CaffeinePro AdvInstaller Setup\bin\publish\*.msi"

