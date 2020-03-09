@echo off

call %~dp0\setup.bat %*

echo MSBuild: %MSBUILD%
echo Build Tool Version: %MSBUILD_TOOL_VERSION%

"%MSBUILD%" %EASYHOOK_ROOT%\build-package.proj /t:Clean;BeforeBuild /tv:%MSBUILD_TOOL_VERSION% %LOGGER%
"%MSBUILD%" %EASYHOOK_ROOT%\build.proj /t:Build /tv:%MSBUILD_TOOL_VERSION% %LOGGER%
"%MSBUILD%" %EASYHOOK_ROOT%\build-package.proj /t:PreparePackage;Package /tv:%MSBUILD_TOOL_VERSION% %LOGGER%
