@echo off

set SETUP_VERSION=0

if "%PATH_BACKUP%" == "" set PATH_BACKUP=%PATH%
nuget >nul
if "%ERRORLEVEL%" NEQ "0" PowerShell.exe -Command "& '%~dp0\ci_install.ps1'"

if "%CURRENT_SETUP_VERSION%" == "%SETUP_VERSION%" echo Skipping environment setup. && goto:eof
set CURRENT_SETUP_VERSION=%SETUP_VERSION%

if "[%APPVEYOR_BUILD_ID%]" NEQ "[]" SET LOGGER=/logger:"C:\Program Files\AppVeyor\BuildAgent\Appveyor.MSBuildLogger.dll"
if "[%APPVEYOR_BUILD_ID%]" == "[]" SET LOGGER=

if "%APPVEYOR_BUILD_FOLDER%" == "" set LOCAL_BUILD=1

if "%APPVEYOR_BUILD_FOLDER%" == "" (
    set APPVEYOR_BUILD_FOLDER=%~dp0..\
)

if "%PLATFORM%" == "" set PLATFORM=Win32
if "%CONFIGURATION%" == "" set CONFIGURATION=netfx3.5-Debug

set BUILD_PLATFORM=x64
if "%PLATFORM%" == "Win32" set BUILD_PLATFORM=x86

set PATH=%~dp0..\Bin;%PATH_BACKUP%

set ADAPTER_PATH=%~dp0..\Packages\Appveyor.TestLogger.2.0.0\build\_common
set TEST_PLATFORM_ROOT=%~dp0..\Packages\Microsoft.TestPlatform.16.5.0\tools\net451\Common7\IDE\Extensions\TestPlatform

set APPVEYOR_TEST_ADAPTER_DLL=Microsoft.VisualStudio.TestPlatform.Extension.Appveyor.TestAdapter.dll
set APPVEYOR_TEST_LOGGER_DLL=Microsoft.VisualStudio.TestPlatform.Extension.Appveyor.TestLogger.dll

set VSTEST=%TEST_PLATFORM_ROOT%\vstest.console.exe
set VSTEST_ARGS=/TestAdapterPath:%ADAPTER_PATH% /TestAdapterPath:%VSTEST_EXTENSIONS% /Logger:Appveyor /Parallel /Platform:%BUILD_PLATFORM% "%APPVEYOR_BUILD_FOLDER%\Build\%CONFIGURATION%\%BUILD_PLATFORM%\EasyHook.Tests.dll"

set VISUAL_STUDIO_PATH=
set BUILD_TOOL_VERSION=

echo.
echo ================================================
echo.

echo AppVeyor Build Worker Image: %APPVEYOR_BUILD_WORKER_IMAGE%

if "%APPVEYOR_BUILD_WORKER_IMAGE%"=="Visual Studio 2013" ( 
    set BUILD_TOOL_VERSION=120 && set VISUAL_STUDIO_NAME=VS2013 
)
if "%APPVEYOR_BUILD_WORKER_IMAGE%"=="Visual Studio 2015" (
    set BUILD_TOOL_VERSION=140 && set VISUAL_STUDIO_NAME=VS2015 
)
if "%APPVEYOR_BUILD_WORKER_IMAGE%"=="Visual Studio 2017" ( 
    set BUILD_TOOL_VERSION=141 && set VISUAL_STUDIO_NAME=VS2017 && C:\Program^ Files^ ^(x86^)\Microsoft^ Visual^ Studio\2017\Community\Common7\Tools\VsDevCmd.bat 
)
if "%APPVEYOR_BUILD_WORKER_IMAGE%"=="Visual Studio 2019" ( 
    set BUILD_TOOL_VERSION=142 && set VISUAL_STUDIO_NAME=VS2019 && C:\Program^ Files^ ^(x86^)\Microsoft^ Visual^ Studio\2017\Community\Common7\Tools\VsDevCmd.bat 
)

if NOT "%VS140COMNTOOLS%"=="" (
  REM Visual Studio 2015
  SET "VISUAL_STUDIO_PATH=%VS140COMNTOOLS%"
  SET "MSBUILD_TOOL_VERSION=14.0"
) else (
  if NOT "%VS120COMNTOOLS%"=="" (
    REM Visual Studio 2013
    SET "VISUAL_STUDIO_PATH=%VS120COMNTOOLS%"
    SET "MSBUILD_TOOL_VERSION=12.0"
  )
)

ECHO Visual Studio Path: %VISUAL_STUDIO_PATH%

::IF "%VISUAL_STUDIO_PATH%"=="" (
::  ECHO Could not find Visual Studio path for supported toolset versions 12.0 or 14.0
:: goto ERROR
::)
::call "%VISUAL_STUDIO_PATH%\..\..\VC\vcvarsall.bat" x86_amd64

copy %ADAPTER_PATH%\%APPVEYOR_TEST_ADAPTER_DLL% %VSTEST_EXTENSIONS%\%APPVEYOR_TEST_ADAPTER_DLL%
copy %ADAPTER_PATH%\%APPVEYOR_TEST_LOGGER_DLL% %VSTEST_EXTENSIONS%\%APPVEYOR_TEST_LOGGER_DLL%

:END
goto:eof

:ERROR
ECHO Error encountered during build.