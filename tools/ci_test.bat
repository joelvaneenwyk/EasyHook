@echo off

if "%APPVEYOR_BUILD_FOLDER%" == "" set APPVEYOR_BUILD_FOLDER=%~dp0..\
if "%PLATFORM%" == "" set PLATFORM=Win32
if "%CONFIGURATION%" == "" set CONFIGURATION=netfx3.5-Debug

set TEST_PLATFORM=x64
if "%PLATFORM%" == "Win32" set TEST_PLATFORM=x86

set PATH=%~dp0..\Bin;%PATH%

set

nuget install -OutputDirectory %~dp0..\Packages Microsoft.TestPlatform -Version 16.5.0

set VSTEST=%~dp0..\Packages\Microsoft.TestPlatform.16.5.0\tools\net451\Common7\IDE\Extensions\TestPlatform\vstest.console.exe

echo vstest.console.exe /Logger:Appveyor /Parallel /Platform:%TEST_PLATFORM% "%APPVEYOR_BUILD_FOLDER%\Build\%CONFIGURATION%\%TEST_PLATFORM%\EasyHook.Tests.dll"
%VSTEST% /Logger:Appveyor /Parallel /Platform:%TEST_PLATFORM% "%APPVEYOR_BUILD_FOLDER%\Build\%CONFIGURATION%\%TEST_PLATFORM%\EasyHook.Tests.dll"