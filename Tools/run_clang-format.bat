@echo off

echo clang-format C#
call %~dp0..\node_modules\.bin\clang-format.cmd -i --glob=**/*.cs

echo clang-format C
call %~dp0..\node_modules\.bin\clang-format.cmd -i --glob=**/*.c

echo clang-format C++
call %~dp0..\node_modules\.bin\clang-format.cmd -i --glob=**/*.cpp

echo clang-format C/C++ Headers
call %~dp0..\node_modules\.bin\clang-format.cmd -i --glob=**/*.h