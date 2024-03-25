@echo off
setlocal EnableExtensions
    set "UA_FEATURES=ANALYZE_BINARIES;SOLUTION_WIDE_SDK_CONVERSION"
    :: set "UA_FEATURES="
    upgrade-assistant analyze M:\projects\EasyHook\EasyHook\EasyHook.csproj
    upgrade-assistant analyze M:\projects\EasyHook\EasyHookSvc\EasyHookSvc.csproj
    upgrade-assistant analyze M:\projects\EasyHook\EasyLoad\EasyLoad.csproj
    upgrade-assistant analyze M:\projects\EasyHook\Examples\FileMon\FileMon.csproj
    upgrade-assistant analyze M:\projects\EasyHook\Examples\FileMonInject\FileMonInject.csproj
    upgrade-assistant analyze M:\projects\EasyHook\Examples\FileMonitorController\FileMonitorController.csproj
    upgrade-assistant analyze M:\projects\EasyHook\Examples\FileMonitorInterceptor\FileMonitorInterceptor.csproj
    upgrade-assistant analyze M:\projects\EasyHook\Examples\FileMonitorInterface\FileMonitorInterface.csproj
    upgrade-assistant analyze M:\projects\EasyHook\Examples\ProcessMonitor\ProcessMonitor.csproj
    upgrade-assistant analyze M:\projects\EasyHook\Examples\ProcMonInject\ProcMonInject.csproj
    upgrade-assistant analyze M:\projects\EasyHook\Test\ComplexParameterInject\ComplexParameterInject.csproj
    upgrade-assistant analyze M:\projects\EasyHook\Test\ComplexParameterTest\ComplexParameterTest.csproj
    upgrade-assistant analyze M:\projects\EasyHook\Test\EasyHook.Tests\EasyHook.Tests.csproj
    upgrade-assistant analyze M:\projects\EasyHook\Test\ManagedTarget\ManagedTarget.csproj
    upgrade-assistant analyze M:\projects\EasyHook\Test\ManagedTest\ManagedTest.csproj
    upgrade-assistant analyze M:\projects\EasyHook\Test\MultipleHooks\MultipleHooks\MultipleHooks.csproj
    upgrade-assistant analyze M:\projects\EasyHook\Test\MultipleHooks\SimpleHook1\SimpleHook1.csproj
    upgrade-assistant analyze M:\projects\EasyHook\Test\MultipleHooks\SimpleHook2\SimpleHook2.csproj
    upgrade-assistant analyze M:\projects\EasyHook\Test\TestFuncHooks\TestFuncHooks.csproj
exit /b 0
