::
:: Helper script that just executes PowerShell install script.
::

@echo off

call %~dp0\setup.bat

PowerShell.exe -Command "& '%~dp0\ci_install.ps1'"

echo.
echo ===============================
echo.

:: Print out all environment variables for reference
set

echo.
echo ===============================
echo.
