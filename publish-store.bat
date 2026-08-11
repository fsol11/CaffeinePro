@echo off
@echo Building Caffeine Pro package for the Microsoft Store
@echo.

set MSBUILD=
for /f "usebackq tokens=*" %%i in (`"%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe" -latest -prerelease -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe`) do set MSBUILD=%%i
if "%MSBUILD%"=="" (
    echo Could not locate MSBuild.exe. Install the Visual Studio "Desktop development with C++" or "Universal Windows Platform development" workload.
    exit /b 1
)

rem AppxBundlePlatforms in the wapproj decides which architectures land in the bundle;
rem /p:Platform here only picks the platform that drives the build.
"%MSBUILD%" "CaffeinePro Setup\CaffeinePro Setup.wapproj" /restore /v:m /nologo /p:Configuration=Release /p:Platform=x86 /p:AppxBundle=Always /p:UapAppxPackageBuildMode=StoreUpload || exit /b 1

@echo.
@echo Upload this file to Partner Center:
@dir /b /s "CaffeinePro Setup\AppPackages\*.msixupload"
@echo.
@echo The bundle is deliberately unsigned - the Store signs it on ingestion. To pre-check with the
@echo Windows App Certification Kit first, rebuild with /p:AppxPackageSigningEnabled=true and a
@echo signing certificate, then run appcert.exe elevated against the resulting _Test bundle.
