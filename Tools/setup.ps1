<#
Helper functions and setup scripts all stored here and used
by other scripts/tools here.
#>

param(
    [Parameter(Position = 0)]
    [string] $Target = $null,
    [switch] $Initialize = $false
)

$script:ToolsDir = split-path -parent $MyInvocation.MyCommand.Definition

$script:EasyHookRootDir = (Get-Item $ToolsDir).Parent
$script:EasyHookRoot = $EasyHookRootDir.FullName
$script:EasyHookSln = Join-Path $EasyHookRoot 'EasyHook.sln'
$script:EasyHookPackages = Join-Path $EasyHookRoot 'Packages'
$script:EasyHookBin = Join-Path $EasyHookRoot 'Bin'

$script:Nuget = Join-Path $EasyHookBin nuget.exe
$script:VsWhereVersion = '3.1.1'
$script:VSWhere = [IO.Path]::Combine($EasyHookPackages, "vswhere.$script:VsWhereVersion", 'tools', 'vswhere.exe')

function Write-Diagnostic {
    param(
        [Parameter(Position = 0, Mandatory = $true, ValueFromPipeline = $true)]
        [string] $Message
    )

    Write-Host $Message -ForegroundColor Green
}

function DownloadNuget() {
    New-Item -ItemType Directory -Force -Path $EasyHookBin | Out-Null
    Set-Alias nuget $script:Nuget -Scope Global
    if (-not (Test-Path $script:Nuget)) {
        $Client = New-Object System.Net.WebClient;
        $Client.DownloadFile('https://dist.nuget.org/win-x86-commandline/latest/nuget.exe', $script:Nuget);
    }
}

function RestoreNugetPackages() {
    . $script:Nuget sources add -name NuGet -Source https://api.nuget.org/v3/index.json

    . $script:Nuget install -NonInteractive -OutputDirectory $script:EasyHookPackages JetBrains.Annotations -Version 2022.3.1 | Out-Null
    . $script:Nuget install -NonInteractive -OutputDirectory $script:EasyHookPackages Microsoft.Build -Version 16.4.0 | Out-Null
    . $script:Nuget install -NonInteractive -OutputDirectory $script:EasyHookPackages Microsoft.TestPlatform -Version 16.5.0 | Out-Null
    . $script:Nuget install -NonInteractive -OutputDirectory $script:EasyHookPackages MSBuildTasks -Version 1.5.0.196 | Out-Null
    . $script:Nuget install -NonInteractive -OutputDirectory $script:EasyHookPackages MSTest.TestAdapter -Version 2.2.10 | Out-Null
    . $script:Nuget install -NonInteractive -OutputDirectory $script:EasyHookPackages MSTest.TestFramework -Version 2.2.10 | Out-Null
    . $script:Nuget install -NonInteractive -OutputDirectory $script:EasyHookPackages vswhere -Version $script:VsWhereVersion | Out-Null

    . $script:Nuget restore -NonInteractive -Force -OutputDirectory $script:EasyHookPackages $script:EasyHookSln
}

# https://github.com/jbake/Powershell_scripts/blob/master/Invoke-BatchFile.ps1
function Invoke-BatchFile {
    param(
        [Parameter(Position = 0, Mandatory = $true, ValueFromPipeline = $true)]
        [string]$Path,
        [Parameter(Position = 1, Mandatory = $true, ValueFromPipeline = $true)]
        [string]$Parameters
    )

    $tempFile = [IO.Path]::GetTempFileName()

    cmd.exe /c " `"$Path`" $Parameters && set > `"$tempFile`" "

    Get-Content $tempFile | Foreach-Object {
        if ($_ -match "^(.*?)=(.*)$") {
            Set-Content "env:\$($matches[1])" $matches[2]
        }
    }

    Remove-Item $tempFile
}

function Die {
    param(
        [Parameter(Position = 0, ValueFromPipeline = $true)]
        [string] $Message
    )

    Write-Host
    Write-Error $Message
    exit 1
}

function Write-Warning {
    param(
        [Parameter(Position = 0, ValueFromPipeline = $true)]
        [string] $Message
    )

    Write-Host
    Write-Host $Message -ForegroundColor Yellow
    Write-Host
}

function Invoke-VisualStudioBuild {
    param(
        [Parameter(Position = 0, ValueFromPipeline = $true)]
        [string] $Sln = $null,

        [Parameter(Position = 1, ValueFromPipeline = $true)]
        [PSCustomObject] $ToolchainInfo,

        [Parameter(Position = 2, ValueFromPipeline = $true)]
        [ValidateSet('netfx4-Debug', 'netfx4-Release')]
        [string] $Configuration,

        [Parameter(Position = 3, ValueFromPipeline = $true)]
        [ValidateSet('Win32', 'x64')]
        [string] $Platform
    )

    Write-Diagnostic "Targeting $Toolchain using configuration $Configuration on platform $Platform"

    # Only configure build environment once
    if ($null -eq $env:EASYHOOK_BUILD_IS_BOOTSTRAPPED) {
        Invoke-BatchFile $($ToolchainInfo.VCVarsAll) $Platform
        $env:EASYHOOK_BUILD_IS_BOOTSTRAPPED = $true
    }

    $Arguments = @(
        "$Sln",
        "/t:rebuild",
        "/tv:$($ToolchainInfo.MSBuildToolVersion)",
        "/p:VisualStudioVersion=$($ToolchainInfo.VisualStudioToolVersion)",
        "/p:Configuration=$Configuration",
        "/p:Platform=$Platform",
        "/p:PlatformToolset=$($ToolchainInfo.Toolchain)",
        "/p:PackageDir=./Deploy;FX4BuildDir=./Build/netfx4-Release/x64",
        "/verbosity:quiet"
    )

    Write-Diagnostic "[cmd] $($ToolchainInfo.MSBuildExe) $Arguments"

    $StartInfo = New-Object System.Diagnostics.ProcessStartInfo
    $StartInfo.FileName = $($ToolchainInfo.MSBuildExe)
    $StartInfo.Arguments = $Arguments

    $StartInfo.EnvironmentVariables.Clear()

    Get-ChildItem -Path env:* | ForEach-Object {
        $StartInfo.EnvironmentVariables.Add($_.Name, $_.Value)
    }

    $StartInfo.UseShellExecute = $false
    $StartInfo.CreateNoWindow = $false
    $StartInfo.RedirectStandardError = $true
    $StartInfo.RedirectStandardOutput = $true

    $Process = New-Object System.Diagnostics.Process
    $Process.StartInfo = $startInfo
    $ProcessCreated = $Process.Start()

    if (!$ProcessCreated) {
        Die "Failed to create process"
    }

    $stdout = $Process.StandardOutput.ReadToEnd()
    $stderr = $Process.StandardError.ReadToEnd()

    $Process.WaitForExit()

    if ($Process.ExitCode -ne 0) {
        Write-Host "stdout: $stdout"
        Write-Host "stderr: $stderr"
        Die "Build failed"
    }
}

function Get-ProcessOutput {
    Param (
        [Parameter(Mandatory = $true)]$FileName,
        $Arguments
    )

    Write-Host "##[cmd] $FileName $Arguments"
    $process = New-Object System.Diagnostics.Process
    $process.StartInfo.UseShellExecute = $false
    $process.StartInfo.RedirectStandardOutput = $true
    $process.StartInfo.RedirectStandardError = $true
    $process.StartInfo.FileName = $FileName
    if ($Arguments) { $process.StartInfo.Arguments = $Arguments }
    $process.Start() | Out-Null

    $StandardError = $process.StandardError.ReadToEnd()
    $StandardOutput = $process.StandardOutput.ReadToEnd()

    $output = New-Object PSObject
    $output | Add-Member -type NoteProperty -name StandardOutput -Value $StandardOutput
    $output | Add-Member -type NoteProperty -name StandardError -Value $StandardError
    return $output
}

function Get-VSWhere {
    param(
        [Parameter(Position = 0, ValueFromPipeline = $true)]
        [string] $Arguments
    )

    if (-not (Test-Path $script:VSWhere)) {
        Die "Unable to find $script:VSWhere"
    }

    $output = Get-ProcessOutput -FileName $script:VSWhere -Arguments "-format json $Arguments"
    $data = $output.StandardOutput | ConvertFrom-Json

    if ($data -is [array]) {
        return $data[0]
    }
    return $data
}

function Invoke-TargetBuild {
    param(
        [Parameter(Position = 0, ValueFromPipeline = $true)]
        [PSCustomObject] $ToolchainInfo
    )

    if ([string]::IsNullOrEmpty($($ToolchainInfo.Target))) {
        Write-Diagnostic "Requires a target to be specified."
        return
    }

    if ($null -eq $($ToolchainInfo.VSInstallationPath)) {
        Write-Warning "Toolchain $($ToolchainInfo.Toolchain) is not installed on your development machine, skipping build."
        Return
    }

    Write-Diagnostic "Visual Studio Installation path: '$($ToolchainInfo.VSInstallationPath)'"
    Write-Diagnostic "Starting build targeting toolchain $($ToolchainInfo.Toolchain)"

    Invoke-VisualStudioBuild $EasyHookSln $ToolchainInfo 'netfx4-Debug' 'x64'
    Invoke-VisualStudioBuild $EasyHookSln $ToolchainInfo 'netfx4-Debug' 'Win32'
    Invoke-VisualStudioBuild $EasyHookSln $ToolchainInfo 'netfx4-Release' 'x64'
    Invoke-VisualStudioBuild $EasyHookSln $ToolchainInfo 'netfx4-Release' 'Win32'

    Write-Diagnostic "Finished build targeting toolchain $($ToolchainInfo.Toolchain)"
}

function Update-AssemblyVersionFile {
    param(
        [Parameter(Position = 0, ValueFromPipeline = $true)]
        [string] $File
    )

    #$Regex = 'public const string AssemblyVersion = "(.*)"';
    $Regex = '\[assembly: AssemblyVersion\("(.*)"\)\]'

    $AssemblyInfo = Get-Content $File
    $NewString = $AssemblyInfo -replace $Regex, "[assembly: AssemblyVersion(""$AssemblyVersion"")]"

    try {
        $NewString | Set-Content $File -Encoding UTF8 | Out-Null
    }
    catch {
        Write-Output ("Failed to update '{0}' as it may be read-only." -f $File)
    }
}

function WriteAssemblyVersion {
    param()

    $Filename = Join-Path $EasyHookRoot EasyHook\Properties\AssemblyInfo.cs
    Write-Diagnostic "$Filename"
    Update-AssemblyVersionFile "$Filename"

    $Filename = Join-Path $EasyHookRoot EasyLoad\Properties\AssemblyInfo.cs
    Update-AssemblyVersionFile "$Filename"
}

Function Add-PathVariable {
    param (
        [string]$addPath
    )
    if (Test-Path $addPath) {
        $regexAddPath = [regex]::Escape($addPath)
        $arrPath = $env:Path -split ';' | Where-Object { $_ -notMatch
            "^$regexAddPath\\?" }
        $env:Path = ($arrPath + $addPath) -join ';'
    }
    else {
        Throw "'$addPath' is not a valid path."
    }
}

Function Get-Toolchain {
    param(
        [string] $Target
    )

    $toolchainArguments = ""

    $WorkerImageOverride = $env:APPVEYOR_BUILD_WORKER_IMAGE
    if (-not ([string]::IsNullOrEmpty($WorkerImageOverride))) {
        $Target = $WorkerImageOverride.replace('Visual Studio ', 'vs')
    }

    # VS2013
    if ($Target -eq 'vs2013') {
        $toolchainArguments += '-legacy -version "[12.0,14.0)"'
    }
    # VS2015
    elseif ($Target -eq 'vs2015') {
        $toolchainArguments += '-legacy -version "[14.0,15.0)"'
    }
    # VS2017
    elseif ($Target -eq 'vs2017') {
        $toolchainArguments += '-version "[15.0,16.0)"'
    }
    # VS2019
    elseif ($Target -eq 'vs2019') {
        $toolchainArguments += '-version "[16.0,17.0)"'
    }
    # VS2022
    elseif ($Target -eq 'vs2022') {
        $toolchainArguments += '-version "[17.0,18.0)"'
    }
    else {
        $toolchainArguments += '-latest'
    }

    $toolchainInfo = Get-VSWhere "$toolchainArguments"
    $VSInstallationPath = $toolchainInfo.installationPath
    $MSBuildExe = Get-VSWhere ($toolchainArguments + ' -requires Microsoft.Component.MSBuild -find MSBuild/**/Bin/MSBuild.exe')

    $versionParts = $toolchainInfo.installationVersion -Split "\."
    $version = "$($versionParts[0]).$($versionParts[1])" -as [double]
    if ($version -ge 17.0) {
        $Target = "vs2022"
    }
    elseif ($version -ge 16.0) {
        $Target = "vs2019"
    }
    elseif ($version -ge 15.0) {
        $Target = "vs2017"
    }
    elseif ($version -ge 14.0) {
        $Target = "vs2015"
    }
    elseif ($version -ge 12.0) {
        $Target = "vs2013"
    }

    $targetMapping = @{
        "nupkg-only" = ""
        "vs2013" = "v120"
        "vs2015" = "v140"
        "vs2017" = "v141"
        "vs2019" = "v142"
        "vs2022" = "v143"
    }
    $toolchain = $targetMapping[$Target]

    switch -Exact ($Target) {
        "vs2013" {
            $VisualStudioToolVersion = "12.0"
            $MSBuildToolVersion = "12.0"
            $VXXCommonTools = Join-Path $VSInstallationPath '.\vc'
        }
        "vs2015" {
            $VisualStudioToolVersion = "14.0"
            $MSBuildToolVersion = "14.0"
            $VXXCommonTools = Join-Path $VSInstallationPath '.\vc'
        }
        "vs2017" {
            $VisualStudioToolVersion = "15.0"
            $MSBuildToolVersion = "14.0"
            $VXXCommonTools = Join-Path $VSInstallationPath '.\vc\auxiliary\build'
        }
        "vs2019" {
            $VisualStudioToolVersion = "16.0"
            $MSBuildToolVersion = "14.0"
            $VXXCommonTools = Join-Path $VSInstallationPath '.\vc\auxiliary\build'
        }
        "vs2022" {
            $VisualStudioToolVersion = "17.0"
            $MSBuildToolVersion = "Current"
            $VXXCommonTools = Join-Path $VSInstallationPath '.\vc\auxiliary\build'
        }
    }

    if ($null -eq $VXXCommonTools -or (-not (Test-Path($VXXCommonTools)))) {
        Die 'Error unable to find any visual studio environment'
    }

    $VCVarsAll = Join-Path $VXXCommonTools 'vcvarsall.bat'
    if (-not (Test-Path $VCVarsAll)) {
        $VCVarsAll = Join-Path $VXXCommonTools 'VsDevCmd.bat'
        if (-not (Test-Path $VCVarsAll)) {
            Die "Unable to find $VCVarsAll"
        }
    }

    if ([string]::IsNullOrEmpty($VSInstallationPath)) {
        Write-Warning "Toolchain $Toolchain is not installed on your development machine, skipping build."
        Return
    }

    $Configuration = $env:Configuration
    if ([string]::IsNullOrEmpty($Configuration)) {
        $Configuration = "netfx4.5-Debug"
    }

    $Platform = $env:Platform
    if ([string]::IsNullOrEmpty($Platform)) {
        $Platform = "x64"
    }

    if ($Platform -eq "Win32") {
        $BuildPlatform = "x86"
    }
    else {
        $BuildPlatform = "x64"
    }

    $output = New-Object PSObject
    $output | Add-Member -type NoteProperty -name VSInstallationPath -Value $VSInstallationPath
    $output | Add-Member -type NoteProperty -name MSBuildExe -Value $MSBuildExe
    $output | Add-Member -type NoteProperty -name Toolchain -Value $toolchain
    $output | Add-Member -type NoteProperty -name Target -Value $Target
    $output | Add-Member -type NoteProperty -name VisualStudioToolVersion -Value $VisualStudioToolVersion
    $output | Add-Member -type NoteProperty -name MSBuildToolVersion -Value $MSBuildToolVersion
    $output | Add-Member -type NoteProperty -name VXXCommonTools -Value $VXXCommonTools
    $output | Add-Member -type NoteProperty -name VCVarsAll -Value $VCVarsAll
    $output | Add-Member -type NoteProperty -name Configuration -Value $Configuration
    $output | Add-Member -type NoteProperty -name Platform -Value $Platform
    $output | Add-Member -type NoteProperty -name BuildPlatform -Value $BuildPlatform

    return $output
}

function Find-Module {
    param (
        [parameter(Mandatory = $true)][string] $name
    )

    $retVal = $true

    if (!(Get-Module -Name $name)) {
        $retVal = Get-Module -ListAvailable | Where-Object { $_.Name -eq $name }

        if ($retVal) {
            try {
                Import-Module $name -ErrorAction SilentlyContinue
            }
            catch {
                $retVal = $false
            }
        }
    }

    return $retVal
}

Function Initialize-Environment {
    param(
        [string] $Target = $null,
        [string] $AssemblyVersion = $null
    )

    $DefaultForeground = (get-host).ui.rawui.ForegroundColor
    $DefaultBackground = (get-host).ui.rawui.BackgroundColor

    Write-Diagnostic "Initializing EasyHook build environment."

    $ContinuousIntegration = ![string]::IsNullOrEmpty($env:CI)
    if ($ContinuousIntegration) {
        Write-Diagnostic "Continuous Integration build."
    }
    else {
        Write-Diagnostic "Local (non-CI) build."
    }

    Write-Diagnostic "EasyHook Tools: '$script:ToolsDir'"
    Write-Diagnostic "EasyHook Packages Taget: '$script:EasyHookPackages'"
    Write-Diagnostic "NuGet: '$script:Nuget'"
    Write-Diagnostic "VSWhere: '$script:VSWhere'"

    # Download local version of NuGet
    Write-Diagnostic "Downloading latest version of NuGet and installing packages."
    DownloadNuget
    RestoreNugetPackages

    $ToolchainInfo = Get-Toolchain $Target

    if ([string]::IsNullOrEmpty($AssemblyVersion)) {
        $AssemblyVersion = "2.8.0.0"
    }

    Write-Host "EasyHook version: $AssemblyVersion"
    Write-Host "Platform: $($ToolchainInfo.Platform)"
    Write-Host "Target: $($ToolchainInfo.Target)"
    Write-Host "Toolchain: $($ToolchainInfo.Toolchain)"
    Write-Host "Visual Studio Tool Version: $($ToolchainInfo.VisualStudioToolVersion)"
    Write-Host "Visual Studio Installation Path: $($ToolchainInfo.VSInstallationPath)"
    Write-Host "MSBuild: $($ToolchainInfo.MSBuildExe)"

    # Update assembly C# files with correct version
    WriteAssemblyVersion

    $BatchEnvironment = Join-Path $EasyHookBin "setup_environment.bat"
    Set-Content -Path $BatchEnvironment -Value "" -Force
    Add-Content $BatchEnvironment "set TOOLCHAIN_VERSION=$($ToolchainInfo.Toolchain)"
    Add-Content $BatchEnvironment "set VISUAL_STUDIO_NAME=$($ToolchainInfo.Target)"
    Add-Content $BatchEnvironment "set VISUAL_STUDIO_PATH=$($ToolchainInfo.VSInstallationPath)"
    Add-Content $BatchEnvironment "set VISUAL_STUDIO_VARS=$($ToolchainInfo.VCVarsAll)"
    Add-Content $BatchEnvironment "set VISUAL_STUDIO_TOOL_VERSION=$($ToolchainInfo.VisualStudioToolVersion)"
    Add-Content $BatchEnvironment "set VISUAL_STUDIO_VARS_ARCH=$($ToolchainInfo.BuildPlatform)"
    Add-Content $BatchEnvironment "set EASYHOOK_TOOLS=$ToolsDir"
    Add-Content $BatchEnvironment "set EASYHOOK_ROOT=$EasyHookRoot"
    Add-Content $BatchEnvironment "set BUILD_PLATFORM=$($ToolchainInfo.BuildPlatform)"

    Add-Content $BatchEnvironment "if ""[%APPVEYOR_BUILD_ID%]"" NEQ ""[]"" SET LOGGER=/logger:""C:\Program Files\AppVeyor\BuildAgent\Appveyor.MSBuildLogger.dll"""
    Add-Content $BatchEnvironment "if ""[%APPVEYOR_BUILD_ID%]"" == ""[]"" SET LOGGER="

    Add-Content $BatchEnvironment "set MSBUILD_TOOL_VERSION=$($ToolchainInfo.MSBuildToolVersion)"
    Add-Content $BatchEnvironment "set MSBUILD_ARGS=/p:VisualStudioVersion=%VISUAL_STUDIO_TOOL_VERSION% %LOGGER%"
    Add-Content $BatchEnvironment "set MSBUILD_EXE=""$($ToolchainInfo.MSBuildExe)"""
    Add-Content $BatchEnvironment "set MSBUILD=%MSBUILD_EXE% %MSBUILD_ARGS%"

    Add-Content $BatchEnvironment "if ""%VSCMD_VER%%__VCVARSALL_TARGET_ARCH%"" == """" echo Calling Visual Studio setup script: ""%VISUAL_STUDIO_VARS%"" %VISUAL_STUDIO_VARS_ARCH%"
    Add-Content $BatchEnvironment "if ""%VSCMD_VER%%__VCVARSALL_TARGET_ARCH%"" == """" call ""%VISUAL_STUDIO_VARS%"" %VISUAL_STUDIO_VARS_ARCH%"

    # We set these back in case Visual Studio or something else modified the setting
    Add-Content $BatchEnvironment "set Configuration=$($ToolchainInfo.Configuration)"
    Add-Content $BatchEnvironment "set Platform=$($ToolchainInfo.Platform)"

    Write-Diagnostic "Installing CoApp."
    $msiPath = Join-Path $script:EasyHookBin "CoApp.Tools.Powershell.msi"
    (New-Object Net.WebClient).DownloadFile('https://easyhook.github.io/downloads/CoApp.Tools.Powershell.msi', $msiPath)
    Get-ProcessOutput -FileName "c:\windows\system32\cmd.exe" -Arguments "/c start /wait msiexec /i ""$msiPath"" /quiet"

    # Add default install directory to module path so we can immediately load the module
    $coAppModulePath = "C:\Program Files (x86)\Outercurve Foundation\Modules"
    $env:PSMODULEPATH = $env:PSMODULEPATH + ";$coAppModulePath"
    Add-Content $BatchEnvironment "set PSMODULEPATH=$($env:PSModulePath)"
    [System.Environment]::SetEnvironmentVariable("PSMODULEPATH", $env:PSModulePath, "User")

    # Import CoApp module (for packaging native NuGet)
    Find-Module "CoApp"

    Add-Content $BatchEnvironment "echo Generated environment batch complete."

    Write-Host "Environment setup complete." -ForegroundColor $DefaultForeground -BackgroundColor $DefaultBackground
}

$ToolchainInfo = Initialize-Environment -Target $Targe

return $ToolchainInfo
