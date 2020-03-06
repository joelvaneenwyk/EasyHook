@echo off

if "%APPVEYOR_BUILD_FOLDER%" == "" set APPVEYOR_BUILD_FOLDER=%~dp0..\
if "%PLATFORM%" == "" set PLATFORM=Win32
if "%CONFIGURATION%" == "" set CONFIGURATION=netfx3.5-Debug

set TEST_PLATFORM=x64
if "%PLATFORM%" == "Win32" set TEST_PLATFORM=x86

set PATH=%~dp0..\Bin;%PATH%

echo.
echo ================================================
echo.
set
echo.
echo ================================================
echo.

nuget install -OutputDirectory %~dp0..\Packages Appveyor.TestLogger -Version 2.0.0
nuget install -OutputDirectory %~dp0..\Packages Microsoft.TestPlatform -Version 16.5.0

set ADAPTER_PATH=%~dp0..\Packages\Appveyor.TestLogger.2.0.0\build\_common
set TEST_PLATFORM_ROOT=%~dp0..\Packages\Microsoft.TestPlatform.16.5.0\tools\net451\Common7\IDE\Extensions\TestPlatform

set VSTEST=%TEST_PLATFORM_ROOT%\vstest.console.exe
set VSTEST_EXTENSIONS=%TEST_PLATFORM_ROOT%\Extensions

set APPVEYOR_TEST_ADAPTER_DLL=Microsoft.VisualStudio.TestPlatform.Extension.Appveyor.TestAdapter.dll
set APPVEYOR_TEST_LOGGER_DLL=Microsoft.VisualStudio.TestPlatform.Extension.Appveyor.TestLogger.dll

copy %ADAPTER_PATH%\%APPVEYOR_TEST_ADAPTER_DLL% %VSTEST_EXTENSIONS%\%APPVEYOR_TEST_ADAPTER_DLL%
copy %ADAPTER_PATH%\%APPVEYOR_TEST_LOGGER_DLL% %VSTEST_EXTENSIONS%\%APPVEYOR_TEST_LOGGER_DLL%

set VSTEST=%~dp0..\Packages\Microsoft.TestPlatform.16.5.0\tools\net451\Common7\IDE\Extensions\TestPlatform\vstest.console.exe
set VSTEST_ARGS=/TestAdapterPath:%ADAPTER_PATH% /TestAdapterPath:%VSTEST_EXTENSIONS% /Logger:Appveyor /Parallel /Platform:%TEST_PLATFORM% "%APPVEYOR_BUILD_FOLDER%\Build\%CONFIGURATION%\%TEST_PLATFORM%\EasyHook.Tests.dll"

echo.
echo ================================================
echo.
echo vstest.console.exe %VSTEST_ARGS%
%VSTEST% %VSTEST_ARGS%
