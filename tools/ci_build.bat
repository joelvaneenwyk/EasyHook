::
:: Build script that sets up environment and handles VS2013, VS2015, VS2017, and VS2019
::

echo Worker image: %APPVEYOR_BUILD_WORKER_IMAGE%

if "%APPVEYOR_BUILD_WORKER_IMAGE%"=="Visual Studio 2013" ( 
    set vsver=120 && set image=VS2013 
)
if "%APPVEYOR_BUILD_WORKER_IMAGE%"=="Visual Studio 2015" (
    set vsver=140 && set image=VS2015 
)
if "%APPVEYOR_BUILD_WORKER_IMAGE%"=="Visual Studio 2017" ( 
    set vsver=150 && set image=VS2017 && C:\Program^ Files^ ^(x86^)\Microsoft^ Visual^ Studio\2017\Community\Common7\Tools\VsDevCmd.bat 
)

:: Print out the environment for reference
set

nuget install MSBuildTasks -Version 1.5.0.196

nuget restore %~dp0..\EasyHook.sln

SET LOGGER=/logger:"C:\Program Files\AppVeyor\BuildAgent\Appveyor.MSBuildLogger.dll"

call %~dp0\findvs.bat

msbuild %~dp0..\build-package.proj /t:Clean;BeforeBuild /tv:%tv% %LOGGER%

msbuild %~dp0..\build.proj /t:Build /tv:%tv% %LOGGER%

msbuild %~dp0..\build-package.proj /t:PreparePackage;Package /tv:%tv% %LOGGER%