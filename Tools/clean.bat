echo off
setlocal EnableDelayedExpansion

call "%~dp0setup.bat"

IF NOT "%vspath%"=="" (
    %MSBUIDL% "%~dp0..\build-package.proj" /t:Clean /tv:%tv%
)

