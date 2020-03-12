@echo off

call %~dp0\setup.bat %*

%POWERSHELL% %POWERSHELL_CONSOLE% "& '%EASYHOOK_TOOLS%\format.ps1' %*";
