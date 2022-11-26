# Almost all CMake files should start with this
# You should always specify a range with the newest
# and oldest tested versions of CMake. This will ensure
# you pick up the best policies.
cmake_minimum_required(VERSION 3.1...3.25)

# This is your project statement. You should always list languages;
# Listing the version is nice here since it sets lots of useful variables
project(
		EasyHook
		VERSION 2.7.8364
		LANGUAGES CSharp CXX)

include(CSharpUtilities)
include(Tools/CMake/EasyHook.cmake)
include(Tools/CMake/EasyHookDll.cmake)
