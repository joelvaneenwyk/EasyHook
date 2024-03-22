@echo off
setlocal EnableDelayedExpansion

echo ##[cmd] call "%~dp0Tools\setup.bat" %*
call "%~dp0Tools\setup.bat" %*
if %ERRORLEVEL% NEQ 0 exit /b %ERRORLEVEL%

if not exist "%POWERSHELL%" (
    echo PowerShell not found. Please install PowerShell 3.0 or later.
    exit /b 88
)

echo ##[cmd] "%POWERSHELL%" %POWERSHELL_CONSOLE% "& '%~dp0Tools\build.ps1' %*;"
"%POWERSHELL%" %POWERSHELL_CONSOLE% "& '%~dp0Tools\build.ps1' %*;"
