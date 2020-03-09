@echo off

call %~dp0\setup.bat %*

echo MSBuild: %MSBUILD%
echo Build Tool Version: %MSBUILD_TOOL_VERSION%

"%MSBUILD%" %EASYHOOK_ROOT%\build-package.proj /t:Clean;BeforeBuild /tv:Current %LOGGER%
"%MSBUILD%" %EASYHOOK_ROOT%\build.proj /t:Build /tv:Current %LOGGER%
"%MSBUILD%" %EASYHOOK_ROOT%\build-package.proj /t:PreparePackage;Package /tv:Current %LOGGER%
