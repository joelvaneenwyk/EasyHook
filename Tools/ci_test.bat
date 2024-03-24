@echo off
setlocal EnableDelayedExpansion

call "%~dp0\setup.bat"

if not exist "%ADAPTER_PATH%\%APPVEYOR_TEST_ADAPTER_DLL%" goto:$SkipAppVeyorCopy
if not exist "%VSTEST_EXTENSIONS%\%APPVEYOR_TEST_ADAPTER_DLL%" goto:$SkipAppVeyorCopy
copy "%ADAPTER_PATH%\%APPVEYOR_TEST_ADAPTER_DLL%" "%VSTEST_EXTENSIONS%\%APPVEYOR_TEST_ADAPTER_DLL%"
copy "%ADAPTER_PATH%\%APPVEYOR_TEST_LOGGER_DLL%" "%VSTEST_EXTENSIONS%\%APPVEYOR_TEST_LOGGER_DLL%"

:$SkipAppVeyorCopy
set VSTEST=vstest.console.exe
set VSTEST_ARGS=/Logger:Appveyor /Platform:%BUILD_PLATFORM% "%EASYHOOK_ROOT%\Build\%CONFIGURATION%\%BUILD_PLATFORM%\EasyHook.Tests.dll"

echo vstest.console.exe %VSTEST_ARGS%
"%VSTEST%" %VSTEST_ARGS%
