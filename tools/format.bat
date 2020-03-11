@echo off

call %~dp0\setup.bat %*

%POWERSHELL% %POWERSHELL_CONSOLE% "& '%EASYHOOK_TOOLS%\format.ps1' %*";

echo eclint (EditorConfig)
call %~dp0..\node_modules\.bin\eclint.cmd fix **/*.cs **/*.cpp **/*.h **/*.c **/*.ps1 **/*.yml **/*.bat
