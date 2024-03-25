# Almost all CMake files should start with this
# You should always specify a range with the newest
# and oldest tested versions of CMake. This will ensure
# you pick up the best policies.
cmake_minimum_required(VERSION 3.15)

# This is your project statement. You should always list languages;
# Listing the version is nice here since it sets lots of useful variables
project(
		EasyHook
		VERSION 2.7.8364
		LANGUAGES ASM_MASM C CXX CSharp)

include(CSharpUtilities)

# Set the C# language version (defaults to 3.0 if not set).
set(CMAKE_CSharp_FLAGS "/langversion:latest")
set(CMAKE_DOTNET_TARGET_FRAMEWORK "net48")
set(CMAKE_DOTNET_SDK "Microsoft.NET.Sdk")

include(Tools/CMake/EasyHook.cmake)
include(Tools/CMake/EasyHookDll.cmake)
