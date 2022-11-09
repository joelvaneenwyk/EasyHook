@echo off

set VSCMD_DEBUG=1
set POWERSHELL=%SystemRoot%\SysWOW64\WindowsPowerShell\v1.0\powershell.exe
set POWERSHELL_CONSOLE=-NoProfile -ExecutionPolicy Bypass -Command

if "[%*]" NEQ "[]" SET TARGET_ARG= -Target %*
if "[%*]" == "[]" SET TARGET_ARG=

"%POWERSHELL%" %POWERSHELL_CONSOLE% "& '%~dp0\setup.ps1'" -Initialize%TARGET_ARG%

call :SetupEnvironment
call :CheckDisassembler

echo Finished initial setup of environment for EasyHook.

exit /b 0

::
:: PowerShell setup script generates a batch script that sets up the environment. We call
:: it from here if it exists.
::
:SetupEnvironment
    if not exist "%~dp0..\Bin\setup_environment.bat" goto:$SetupEnvironment.error

    call "%~dp0..\Bin\setup_environment.bat"

    set ADAPTER_PATH=%~dp0..\Packages\Appveyor.TestLogger.2.0.0\build\_common
    set TEST_PLATFORM_ROOT=%~dp0..\Packages\Microsoft.TestPlatform.16.5.0\tools\net451\Common7\IDE\Extensions\TestPlatform

    set APPVEYOR_TEST_ADAPTER_DLL=Microsoft.VisualStudio.TestPlatform.Extension.Appveyor.TestAdapter.dll
    set APPVEYOR_TEST_LOGGER_DLL=Microsoft.VisualStudio.TestPlatform.Extension.Appveyor.TestLogger.dll

    set VSTEST=%TEST_PLATFORM_ROOT%\vstest.console.exe
    set VSTEST_ARGS=/TestAdapterPath:"%ADAPTER_PATH%" /TestAdapterPath:"%VSTEST_EXTENSIONS%" /Logger:Appveyor /Parallel /Platform:%BUILD_PLATFORM% "%EASYHOOK_ROOT%\Build\%CONFIGURATION%\%BUILD_PLATFORM%\EasyHook.Tests.dll"

    goto:$SetupEnvironment.done

    :$SetupEnvironment.error
    echo Environment batch is missing.
    exit /b 1

    :$SetupEnvironment.done
exit /b 0

:CheckDisassembler
    :: Printing this out to make sure the tool is findable
    ildasm.exe /? > nul 2>&1
    if errorlevel 1 goto:$CheckDisassembler.error
    echo Found 'ildasm.exe'
    goto:$CheckDisassembler.done

    :$CheckDisassembler.error
    echo Failed to find 'ildasm.exe'
    exit /b 1

    :$CheckDisassembler.done
exit /b 0
