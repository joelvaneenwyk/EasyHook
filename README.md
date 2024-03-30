# EasyHook

> This is a fork of the [EasyHook](<https://github.com/EasyHook/EasyHook>) project.

## Reinvention of Windows API Hooking

`EasyHook` supports extending (hooking) unmanaged code (APIs) with pure managed ones, from within a fully managed environment on both 32-bit and 64-bit versions of Windows Vista x64, Windows Server 2008 x64, Windows 7, Windows 8.1, Windows 10, and Windows 11.

`EasyHook` currently supports injecting assemblies built for .NET Framework 4.0+ and can inject native DLLs.

## EasyHook homepage

For more information head to the EasyHook site at [easyhook.github.io](https://easyhook.github.io/)

## NuGet

> [nuget.org/packages/EasyHook](https://www.nuget.org/packages/EasyHook)

For native C++ apps there is also a native NuGet package: [nuget.org/packages/EasyHookNativePackages](https://www.nuget.org/packages/EasyHookNativePackage)

## Bug reports or questions

Reporting bugs is the only way to get them fixed and help other users of the library! If an issue isn't getting addressed, try [raising a bounty for it](https://www.bountysource.com/teams/easyhook/issues).

Report issues at [github.com/EasyHook](https://github.com/EasyHook/EasyHook/issues).

## Building

Requirements:

* Visual Studio 2019 or later
* [MSBuild Community Tasks](https://github.com/loresoft/msbuildtasks)
* Windows 8.1 SDK
* C++ ATL for latest v142 build tools (x86 & x64)

### EasyHook Service

You need to install the Windows Driver Kit (WDK) for your version of windows. See [Download the Windows Driver Kit (WDK) - Windows drivers | Microsoft Learn](https://learn.microsoft.com/en-us/windows-hardware/drivers/download-the-wdk) for up-to-date steps.

These are the steps for Windows 11:

1. Install Visual Studio 2022
2. Install [Windows SDK](https://developer.microsoft.com/en-us/windows/downloads/windows-sdk/)
3. Install [WDK](https://go.microsoft.com/fwlink/?linkid=2196230)

### Build Binaries

Use `build.bat` to build binaries for all configurations (.NET 4.0, `Debug`/`Release`, and `x86`/`x64`).

* `./Debug/x86`
* `./Debug/x64`
* `./Release/x86`
* `./Release/x64`

### Build Release Package

Use `build-package.bat` to set version information, build binaries for all
configurations and ZIP Release builds and Source within `./Deploy`.

Generates archives:

* `./Deploy/EasyHook-#.#.#.#-Binaries.zip`
* `./Deploy/EasyHook-#.#.#.#-Source.zip`

Containing the following files:

* `./Deploy/NetFX4.0/*`
* `./Deploy/Source/*`

### Installing and Building EasyHook with `vcpkg`

You can download and install easyhook using the [vcpkg dependency manager](https://github.com/Microsoft/vcpkg):

* `git clone https://github.com/Microsoft/vcpkg.git`
* `cd vcpkg`
* `./bootstrap-vcpkg.sh`
* `./vcpkg integrate install`
* `vcpkg install easyhook`

The easyhook port in vcpkg is kept up to date by Microsoft team members and community contributors. If the version is out of date, please create an issue or pull request on the [vcpkg repository](https://github.com/Microsoft/vcpkg).

## External Libraries

`EasyHook` includes the `UDIS86` library Copyright (c) 2002-2012, Vivek Thampi <vivek.mt@gmail.com>. See [udis86-LICENSE.txt](DriverShared/Disassembler/udis86-LICENSE.txt) for license details. Minor modifications have been made for it to compile with `EasyHook`.

More information about `UDIS86` can be found at [github.com/vmt/udis86](https://github.com/vmt/udis86).
