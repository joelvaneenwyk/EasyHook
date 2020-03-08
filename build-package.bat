@echo off

call %~dp0\Tools\setup.bat
call %~dp0\Tools\findvs.bat

nuget install -OutputDirectory %~dp0%\Packages MSBuildTasks -Version 1.5.0.196
nuget restore %~dp0\EasyHook.sln
msbuild build-package.proj /t:Clean;BeforeBuild /tv:%tv% %LOGGER%
msbuild build.proj /t:Build /tv:%tv% %LOGGER%
msbuild build-package.proj /t:PreparePackage;Package /tv:%tv% %LOGGER%
