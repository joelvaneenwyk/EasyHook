@echo off

call %~dp0\setup.bat %*

echo MSBuild: %MSBUILD%
echo Build Tool Version: %MSBUILD_TOOL_VERSION%

echo Calling Visual Studio setup script: "%VISUAL_STUDIO_VARS%" %VISUAL_STUDIO_VARS_ARCH%
call "%VISUAL_STUDIO_VARS%" %VISUAL_STUDIO_VARS_ARCH%

"%MSBUILD%" %EASYHOOK_ROOT%\build-package.proj /t:Clean;BeforeBuild /tv:Current %LOGGER%
"%MSBUILD%" %EASYHOOK_ROOT%\build.proj /t:Build /tv:Current %LOGGER%
"%MSBUILD%" %EASYHOOK_ROOT%\build-package.proj /t:PreparePackage;Package /tv:Current %LOGGER%
