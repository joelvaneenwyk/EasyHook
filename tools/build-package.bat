@echo off

call %~dp0\setup.bat %*

set MSBUILDS_ARGS=/tv:%MSBUILD_TOOL_VERSION% /p:VisualStudioVersion=%VISUAL_STUDIO_TOOL_VERSION% %LOGGER%

echo MSBuild: %MSBUILD% %MSBUILDS_ARGS%
echo Build Tool Version: %MSBUILD_TOOL_VERSION%
echo Visual Studio Tool Version: %VISUAL_STUDIO_TOOL_VERSION%

"%MSBUILD%" %EASYHOOK_ROOT%\build-package.proj /t:Clean;BeforeBuild %MSBUILDS_ARGS%
"%MSBUILD%" %EASYHOOK_ROOT%\build.proj /t:Build %MSBUILDS_ARGS%
"%MSBUILD%" %EASYHOOK_ROOT%\build-package.proj /t:PreparePackage;Package %MSBUILDS_ARGS%
