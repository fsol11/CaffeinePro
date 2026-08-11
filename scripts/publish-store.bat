@echo off
@echo Building Caffeine Pro package for the Microsoft Store
@echo.

rem Everything below is relative to the repository root, so move there regardless of where the
rem caller ran this from. %~dp0 is this script's folder, one level under the root. setlocal makes
rem cmd restore the original directory when the script ends, including via the exit /b paths below.
setlocal
cd /d "%~dp0.."

set MSBUILD=
for /f "usebackq tokens=*" %%i in (`"%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe" -latest -prerelease -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe`) do set MSBUILD=%%i
if "%MSBUILD%"=="" (
    echo Could not locate MSBuild.exe. Install the Visual Studio "Desktop development with C++" or "Universal Windows Platform development" workload.
    exit /b 1
)

rem Clear previous output first. A solution build also drops a single-architecture .msixupload in
rem here, and two candidates means someone eventually uploads the wrong one to Partner Center.
if exist "CaffeinePro Setup\AppPackages" rd /s /q "CaffeinePro Setup\AppPackages"

rem Also clear the packaging intermediates. MSBuild does not notice when an image is removed from
rem the Images\*.png glob, so the generated mapping file keeps naming the deleted file and makeappx
rem fails with "The mapping file can't be parsed". This runs rarely, so correctness beats speed.
if exist "CaffeinePro Setup\obj" rd /s /q "CaffeinePro Setup\obj"
if exist "CaffeinePro Setup\bin" rd /s /q "CaffeinePro Setup\bin"

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
