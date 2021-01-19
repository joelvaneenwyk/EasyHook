@ECHO OFF

call "%~dp0setup.bat" %*

%POWERSHELL% %POWERSHELL_CONSOLE% "& '%EASYHOOK_TOOLS%\build.ps1' %*";
