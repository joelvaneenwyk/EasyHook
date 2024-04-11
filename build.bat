@echo off & goto:$Main

:GetEnv
setlocal EnableDelayedExpansion
    for /f "tokens=*" %%i in ('dir /b /s /a:d /o:n "%ProgramFiles%\Microsoft Visual Studio\2022\*"') do (
        set "_devenv=%%~i\Common7\Tools\VsDevCmd.bat"
        if exist "!_devenv!" set "_devenv=!_devenv!"
        if exist "!_devenv!" goto:$GetEnvDone
    )
    :$GetEnvDone
    echo DevEnv: "!_devenv!"
endlocal & (
    set "devenv=%_devenv%"
    exit /b 0
)

:$Main
    call :GetEnv

    if not exist "%devenv%" (
        echo [ERROR] Failed to find Visual Studio 2022.
        exit /b 97
    )

    :: sudo reg add "HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\MSBuild\4.0" /v DebuggerEnabled /d true
    msbuild --version >nul 2>&1
    if errorlevel 1 (
        echo ##command "%devenv%"
        call "%devenv%"
    )

    call msbuild %*
goto:eof
