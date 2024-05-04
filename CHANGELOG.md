# EasyHook

## Unreleased

### Code Cleanup and Refactoring

- Clean up scripts, build debug by default, add launch setting for VSCode
- Clean up the README
- Fix the README and dependencies
- Add header and run ReSharper cleanup
- Remove unused code and clean up remaining code
- Clean up code further
- Clean up code and add ReSharper config
- Clean up code
- Remove unused imports, add editorconfig, fix solution global files, etc
- Add more comments and refactor
- Format and clean up whitespace
- Fix/update formatting
- Change trivial whitespace
- Change encoding/whitespace e.g., removing BOM
- Change trivial whitespace
- Change trivial whitespace in C# source
- Update C# files with automatic and trivial formatting
- Update README formatting
- Run pre-commit
- Add dictionary

### Build and CI/CD Configuration

- Remove 'build_me.txt' and migrate over vcpkg info
- Add support for latest version of VS2019
- Fix CI build
- Add basic GitHub Action for msbuild default
- Add all variants to build
- Add solution file to GitHub action
- Build for vs2015, vs2017, and vs2019
- Add actions for each variant to build
- Use Windows image version to change Visual Studio version
- Fix typo in workflow
- Update workflow names to differentiate
- Fix driver build action configuration
- Specify generic Windows target instead of specific one as it depends on Windows version
- Remove netfx3.5 and switch to VS2019 by default
- Switch to .NET Framework v4.5.2 since we use VS2019 by default now
- Remove 'netfx3.5' config and 'windows-2016' image from GitHub actions
- Remove AppVeyor configs as we switched to GitHub actions
- Add preliminary build support for VS2022
- Upgrade to VS2022 and remove Windows 7/8.1 support from 'EasyHookSys' and 'TestDriver' projects
- Remove unsupported configs from EasyHook Driver GitHub actions
- Specify 'cmd' shell explicitly to fix package CI build
- Fix typo in GitHub package action
- Update GitHub action
- Fix workflow syntax
- Combine workflows together
- Fix driver build
- Specify the range inside the matrix
- Upgrade projects to .NET Framework v4.8.1
- Upgrade C++ projects to .NET Framework v4.8.1
- Fix build by using 'windows-2019' image
- Combine package step with build
- Use Framework v4.8 as that is available on 'windows-2019' runner
- Avoid building unsupported targets
- Disable more unsupported build and ignore errors in xcopy
- Consolidate to have only Debug and Release configs
- Fix xcopy errors
- Avoid building as errors
- Clarify build names
- Use 'x86' as platform
- Fix driver
- Ignore build older
- Clean up workflow
- Remove hardcoded paths per project
- Clean up copy step
- Fix more
- Fix more obvious
- Set driver version
- Fix more
- Add nit condition
- Fix build
- Fix more
- Revert back to nuget restore
- Remove feature branches from auto-builds
- Disable analysis
- Disable inf2cat
- Disable broken targets
- Disable tests
- Upgrade all projects to Sdk style
- Remove Packages folder
- Update solution projects
- Fix build script and remove deprecated upgrade script
- Update scripts
- Fix some issues after EasyHookDll additions
- Fix the build by restoring x64 config properties in easy hook dll
- Fix build: x86 was not building x64 dll

### Code and Project Updates

- Copy over PDBs
- Support capturing stdout/stderr
- Commit intermediate/ugly but redirects are working
- Return the exit code and no longer support .NET 3.5 with concurrent addition
- Use callback for all prints in remote hook process
- Re-enable netfx3.5 support but remove dependency on concurrent generics
- Generate PDB files in release for EasyLoad
- Fix file hook unit tests by using singleton
- Fix easy hook name warning by having two definitions
- Add enum for injection options
- Set ReSharper version to C# 6.0 to align with VS2015
- Set ReSharper to check for C# 7.0 compliance instead of C# 6.0 since we can use more modern C# features now
- Add reference to JetBrains.Annotations so we can access additional attributes like PublicAPI
- Update DotSettings files for ReSharper
- Add new packages.config for tests
- Add missing 'app.config' files for 'FileMon' and 'ProcessMonitor'
- Add double quotes around adapter path in 'setup.bat' to support paths with spaces
- Clean up project and solution
- Remove AppVeyor config files from solution
- Remove unused sections in '.editorconfig'
- Update README.md
- Switch to using Package References
- Clean up config
- Clean up build script
- Add CMake config files for 'EasyHook' and 'EasyHookDll'
- Disable precompiled headers in 'EasyHookDll'
- Remove Win32 config from drivers/services
- Add 'PnpLockdown=1' to 'TestDriver.inf' to fix warning
- Disable warning about obsolete usage of 'SignalEndpointReady'
- Upgrade GitHub actions to use 'actions/checkout@v3'
- Add short section to README on how to setup WDK
- Add IDEA config files for Rider and CLion
- Update CMake configs
- Fix CMake configuration settings to handle x86 and x64
- Fix some build warnings/errors
- Add 'clang-format' config that attempts to match existing code
- Clean up '.editorconfig' settings
- Add intermediate folders to exclude list for JetBrains
- Update VSCode
- Update LICENSE format
- Update README
- Add/update 'app.config' files
- Update CMake settings
- Ignore CMake intermediate output
- Add driver projects to solution
- Ignore 'TestResults' folder
- Add packages.lock.json files
- Add directory build props
- Move more shared properties to directory props
- Add target machine to build props
- Add helper build script to root
- Remove more superfluous attributes
- Migrate easy hook tests to SDK style project
- Remove platform toolset from getting set
- Clean up config settings
- Update README
- Add 'Directory.Build.targets' and move some properties there
- Rename EasyHookDll project to "EasyHookDll_64" before creating 32-bit version
- Add 32-bit only version of EasyHook project
- Add gitattributes
- Enforce utf-8 on c# files to remove bom
- Re-normalize files
- Add precommit
- Add precommit to remove byte order marker (BOM)
- Remove BOM
- Submit partial working version and will fix EasyHookDll next

### Initial Project Setup

- Add 'CHANGELOG.md'
- Add file monitor unit tests

### Minor Tweaks and Fixes

- Tweak minor
- Fix casing of 'tools' to 'Tools'
- Miss 'DotSettings' files and additional 'netfx3.5' removal
- Clean up scripts
- Add readonly modifier
- Update workflow names to differentiate
- Update GitHub action
