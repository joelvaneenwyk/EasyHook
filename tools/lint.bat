@echo off

call %~dp0..\node_modules\.bin\eclint.cmd check **/*.cs **/*.cpp **/*.h **/*.c
