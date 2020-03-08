@echo off

call %~dp0\setup.bat

:: This calls the newest version of vstest.console.exe but for some reason even with correct adapater
:: paths it is not able to find Appveyor
::pushd %TEST_PLATFORM_ROOT%
::echo vstest.console.exe %VSTEST_ARGS%
::%VSTEST% %VSTEST_ARGS%
::popd

::set VSTEST=%TEST_PLATFORM_ROOT%\vstest.console.exe
::set VSTEST_EXTENSIONS=%TEST_PLATFORM_ROOT%\Extensions
::set VSTEST_ARGS=/TestAdapterPath:%ADAPTER_PATH% /TestAdapterPath:%VSTEST_EXTENSIONS% /Logger:Appveyor /Parallel /Platform:%BUILD_PLATFORM% "%APPVEYOR_BUILD_FOLDER%\Build\%CONFIGURATION%\%BUILD_PLATFORM%\EasyHook.Tests.dll"

set VSTEST=vstest.console.exe
set VSTEST_ARGS=/Logger:Appveyor /Platform:%BUILD_PLATFORM% "%APPVEYOR_BUILD_FOLDER%\Build\%CONFIGURATION%\%BUILD_PLATFORM%\EasyHook.Tests.dll"
echo vstest.console.exe %VSTEST_ARGS%
%VSTEST% %VSTEST_ARGS%
