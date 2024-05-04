::
:: Helper script that executes PowerShell install script.
::
@echo off
setlocal EnableDelayedExpansion

echo.
echo ===============================
echo. EasyHook CI Install Script
echo ===============================
echo.

echo Platform: %Platform%
echo Configuration: %Configuration%

call "%~dp0\setup.bat" %*

echo.
echo ===============================
echo.

echo Platform: '%Platform%'
echo Configuration: '%Configuration%'
echo AppVeyor Build Worker Image: '%APPVEYOR_BUILD_WORKER_IMAGE%'
echo Visual Studio Name: '%VISUAL_STUDIO_NAME%'
echo Visual Studio Path: '%VISUAL_STUDIO_PATH%'
echo Toolchain Version: '%TOOLCHAIN_VERSION%'
echo MSBuild Tool Version: '%MSBUILD_TOOL_VERSION%'

echo.
echo ===============================
