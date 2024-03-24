@echo off & goto:$Main

:GetEnv
setlocal EnableDelayedExpansion
    for /f "tokens=*" %%i in ('dir /b /s /a:d /o:n "%ProgramFiles%\Microsoft Visual Studio\2022\*"') do (
        set "_devenv=%%~i\Common7\Tools\VsDevCmd.bat"
        if exist "!_devenv!" set "_devenv=!_devenv!"
    )
endlocal & (
    set "devenv=%_devenv%"
    exit /b 0
)

:$Main
    call :GetEnv
    :: sudo reg add "HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\MSBuild\4.0" /v DebuggerEnabled /d true
    msbuild --version >nul 2>&1
    if errorlevel 1 (
        echo ##command %devenv%
        call "%devenv%"
    )

    call msbuild %*
    :: echo ##[cmd] call "%~dp0Tools\setup.bat" %*
    :: call "%~dp0Tools\setup.bat" %*
    :: if %ERRORLEVEL% NEQ 0 exit /b %ERRORLEVEL%
    :: if not exist "%POWERSHELL%" (
    ::     echo PowerShell not found. Please install PowerShell 3.0 or later.
    ::     exit /b 88
    :: )
    :: echo ##[cmd] "%POWERSHELL%" %POWERSHELL_CONSOLE% "& '%~dp0Tools\build.ps1' %*;"
    :: "%POWERSHELL%" %POWERSHELL_CONSOLE% "& '%~dp0Tools\build.ps1' %*;"
exit /b 0
