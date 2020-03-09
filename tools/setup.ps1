<#
Helper functions and setup scripts all stored here and used
by other scripts/tools here. 
#>

param(
    [switch] $Initialize = $false
)

$script:ToolsDir = split-path -parent $MyInvocation.MyCommand.Definition
$script:EasyHookRootDir = (Get-Item $ToolsDir).Parent
$script:EasyHookRoot = $EasyHookRootDir.FullName
$script:EasyHookSln = Join-Path $EasyHookRoot 'EasyHook.sln'
$script:EasyHookPackages = Join-Path $EasyHookRoot 'Packages'
$script:EasyHookBin = Join-Path $EasyHookRoot 'Bin'
$script:VSWhere = [IO.Path]::Combine($EasyHookPackages, 'vswhere.2.8.4', 'tools', 'vswhere.exe')
$script:VSInstallationPath = $null

function Write-Diagnostic {
    param(
        [Parameter(Position = 0, Mandatory = $true, ValueFromPipeline = $true)]
        [string] $Message
    )

    Write-Host $Message -ForegroundColor Green
}

function DownloadNuget() {
    New-Item -ItemType Directory -Force -Path $EasyHookBin
    $script:Nuget = Join-Path $EasyHookBin nuget.exe
    Set-Alias nuget $script:Nuget -Scope Global -Verbose
    if (-not (Test-Path $script:Nuget)) {
        $Client = New-Object System.Net.WebClient;
        $Client.DownloadFile('http://nuget.org/nuget.exe', $script:Nuget);
    }
}

function RestoreNugetPackages() {
    $Nuget = Join-Path $EasyHookBin nuget.exe
    . $Nuget restore -OutputDirectory $EasyHookPackages $EasyHookSln
    . $Nuget install -OutputDirectory $EasyHookPackages MSBuildTasks -Version 1.5.0.196
    . $Nuget install -OutputDirectory $EasyHookPackages vswhere -Version 2.8.4
    . $Nuget install -OutputDirectory $EasyHookPackages Microsoft.TestPlatform -Version 16.5.0
    . $Nuget install -OutputDirectory $EasyHookPackages Appveyor.TestLogger -Version 2.0.0
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

function Warn {
    param(
        [Parameter(Position = 0, ValueFromPipeline = $true)]
        [string] $Message
    )

    Write-Host
    Write-Host $Message -ForegroundColor Yellow
    Write-Host
}

function TernaryReturn {
    param(
        [Parameter(Position = 0, ValueFromPipeline = $true)]
        [bool] $Yes,
        [Parameter(Position = 1, ValueFromPipeline = $true)]
        $Value,
        [Parameter(Position = 2, ValueFromPipeline = $true)]
        $Value2
    )

    if ($Yes) {
        return $Value
    }
    
    $Value2
}

function Msvs {
    param(
        [Parameter(Position = 0, ValueFromPipeline = $true)]
        [string] $Sln = $null,
        [ValidateSet('v120', 'v140', 'v141', 'v142')]
        [Parameter(Position = 1, ValueFromPipeline = $true)]
        [string] $Toolchain, 

        [Parameter(Position = 2, ValueFromPipeline = $true)]
        [ValidateSet('netfx3.5-Debug', 'netfx3.5-Release', 'netfx4-Debug', 'netfx4-Release')]
        [string] $Configuration, 

        [Parameter(Position = 3, ValueFromPipeline = $true)]
        [ValidateSet('Win32', 'x64')]
        [string] $Platform
    )

    Write-Diagnostic "Targeting $Toolchain using configuration $Configuration on platform $Platform"

    $VisualStudioVersion = $null
    $VXXCommonTools = $null

    $output = Get-ProcessOutput -FileName $script:VSWhere -Args '-latest -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe'
    $MSBuildExe = ($output.StandardOutput -split '\n')[0]

    switch -Exact ($Toolchain) {
        'v120' {
            $VisualStudioVersion = '12.0'
            $VXXCommonTools = Join-Path $VSInstallationPath '.\vc'
        }
        'v140' {
            $VisualStudioVersion = '14.0'
            $VXXCommonTools = Join-Path $VSInstallationPath '.\vc'
        }
        'v141' {
            $VisualStudioVersion = '14.1'
            $VXXCommonTools = Join-Path $VSInstallationPath '.\vc\auxiliary\build'
        }
        'v142' {
            $VisualStudioVersion = '14.2'
            $VXXCommonTools = Join-Path $VSInstallationPath '.\vc\auxiliary\build'
        }
    }

    if ($null -eq $VXXCommonTools -or (-not (Test-Path($VXXCommonTools)))) {
        Die 'Error unable to find any visual studio environment'
    }

    $VCVarsAll = Join-Path $VXXCommonTools vcvarsall.bat
    if (-not (Test-Path $VCVarsAll)) {
        Die "Unable to find $VCVarsAll"
    }

    # Only configure build environment once
    if ($null -eq $env:EASYHOOK_BUILD_IS_BOOTSTRAPPED) {
        Invoke-BatchFile $VCVarsAll $Platform
        $env:EASYHOOK_BUILD_IS_BOOTSTRAPPED = $true
    }

    $Arch = TernaryReturn ($Platform -eq 'x64') 'x64' 'win32'

    $Arguments = @(
        "$Sln",
        "/t:rebuild",
        "/tv:Current",
        "/p:VisualStudioVersion=$VisualStudioVersion",
        "/p:Configuration=$Configuration",
        "/p:Platform=$Arch",
        "/p:PlatformToolset=$Toolchain",
        "/p:PackageDir=.\Deploy;FX35BuildDir=.\Build\netfx3.5-Release\x64;FX4BuildDir=.\Build\netfx4-Release\x64",
        "/verbosity:quiet"
    )

    Write-Diagnostic "MSBuild Executable: $MSBuildExe $Arguments"

    $StartInfo = New-Object System.Diagnostics.ProcessStartInfo
    $StartInfo.FileName = $MSBuildExe
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
    $Process.Start()
    
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
        $Args
    )
    
    $process = New-Object System.Diagnostics.Process
    $process.StartInfo.UseShellExecute = $false
    $process.StartInfo.RedirectStandardOutput = $true
    $process.StartInfo.RedirectStandardError = $true
    $process.StartInfo.FileName = $FileName
    if ($Args) { $process.StartInfo.Arguments = $Args }
    $out = $process.Start()
    
    $StandardError = $process.StandardError.ReadToEnd()
    $StandardOutput = $process.StandardOutput.ReadToEnd()
    
    $output = New-Object PSObject
    $output | Add-Member -type NoteProperty -name StandardOutput -Value $StandardOutput
    $output | Add-Member -type NoteProperty -name StandardError -Value $StandardError
    return $output
}

function FindVisualStudio {
    param(
        [Parameter(Position = 0, ValueFromPipeline = $true)]
        [string] $Toolchain
    )

    Write-Diagnostic $script:VSWhere

    $args = ""

    # VS2013
    if ($Toolchain -eq 'v120') {
        $args += '-legacy -version "[12.0,14.0)"'
    }
    
    # VS2015
    if ($Toolchain -eq 'v140') {
        $args += '-legacy -version "[14.0,15.0)"'
    }
    
    # VS2017
    if ($Toolchain -eq 'v141') {
        $args += '-version "[15.0,16.0)"'
    }
    
    # VS2019
    if ($Toolchain -eq 'v142') {
        $args += '-version "[16.0,17.0)"'
    }
    
    $args += ' -property installationPath'

    $output = Get-ProcessOutput -FileName $script:VSWhere -Args $args
    $script:VSInstallationPath = $output.StandardOutput.Trim()
}

function VSX {
    param(
        [ValidateSet('v120', 'v140', 'v141', 'v142')]
        [Parameter(Position = 0, ValueFromPipeline = $true)]
        [string] $Toolchain
    )

    FindVisualStudio "$Toolchain"
    
    if ($null -eq $script:VSInstallationPath) {
        Warn "Toolchain $Toolchain is not installed on your development machine, skipping build."
        Return
    }
    
    Write-Diagnostic "Visual Studio Installation path: $VSInstallationPath"
    Write-Diagnostic "Starting build targeting toolchain $Toolchain"

    Msvs "$EasyHookSln" "$Toolchain" 'netfx3.5-Release' 'x64'
    Msvs "$EasyHookSln" "$Toolchain" 'netfx3.5-Release' 'Win32'
    Msvs "$EasyHookSln" "$Toolchain" 'netfx4-Release' 'x64'
    Msvs "$EasyHookSln" "$Toolchain" 'netfx4-Release' 'Win32'
    
    # <MSBuild Projects="EasyHook.sln" Properties="Configuration=netfx3.5-Debug;Platform=x64" />
    # <MSBuild Projects="EasyHook.sln" Properties="Configuration=netfx3.5-Debug;Platform=Win32" />
    # <MSBuild Projects="EasyHook.sln" Properties="Configuration=netfx3.5-Release;Platform=x64" />
    # <MSBuild Projects="EasyHook.sln" Properties="Configuration=netfx3.5-Release;Platform=Win32" />
    # <MSBuild Projects="EasyHook.sln" Properties="Configuration=netfx4-Debug;Platform=x64" />
    # <MSBuild Projects="EasyHook.sln" Properties="Configuration=netfx4-Debug;Platform=Win32" />
    # <MSBuild Projects="EasyHook.sln" Properties="Configuration=netfx4-Release;Platform=x64" />
    # <MSBuild Projects="EasyHook.sln" Properties="Configuration=netfx4-Release;Platform=Win32" />

    Write-Diagnostic "Finished build targeting toolchain $Toolchain"
}

function WriteAssemblyVersionForFile {
    param(
        [Parameter(Position = 0, ValueFromPipeline = $true)]
        [string] $File
    )
    
    #$Regex = 'public const string AssemblyVersion = "(.*)"';
    $Regex = '\[assembly: AssemblyVersion\("(.*)"\)\]'
    
    $AssemblyInfo = Get-Content $File
    $NewString = $AssemblyInfo -replace $Regex, "[assembly: AssemblyVersion(""$AssemblyVersion"")]"
    
    $NewString | Set-Content $File -Encoding UTF8
}

function WriteAssemblyVersion {
    param()

    $Filename = Join-Path $EasyHookRoot EasyHook\Properties\AssemblyInfo.cs
    Write-Diagnostic "$Filename"
    WriteAssemblyVersionForFile "$Filename"
    $Filename = Join-Path $EasyHookRoot EasyLoad\Properties\AssemblyInfo.cs
    WriteAssemblyVersionForFile "$Filename"
}

function Nupkg {
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
    
    switch -Exact ($Target) {
        "nupkg-only" {
            $toolchain = ""
        }
        "vs2013" {
            $toolchain = "v120"
        }
        "vs2015" {
            $toolchain = "v140"
        }
        "vs2017" {
            $toolchain = "v141"
        }
        "vs2019" {
            $toolchain = "v142"
        }
    }

    return $toolchain
}
Function Initialize-Environment {
    param(
        [string] $Target = "vs2017",
        [string] $AssemblyVersion = "2.8.0.0" 
    )

    if ($env:APPVEYOR_BUILD_WORKER_IMAGE -eq "Visual Studio 2013") {
        $Target = "vs2013"
    }
    if ($env:APPVEYOR_BUILD_WORKER_IMAGE -eq "Visual Studio 2015") {
        $Target = "vs2015"
    }
    if ($env:APPVEYOR_BUILD_WORKER_IMAGE -eq "Visual Studio 2017") { 
        $Target = "vs2017"
    }
    if ($env:APPVEYOR_BUILD_WORKER_IMAGE -eq "Visual Studio 2019") {
        $Target = "vs2019"
    }

    $Configuration = $env:CONFIGURATION
    if (!$Configuration) {
        $Configuration = "netfx3.5-Debug"
    } 

    $Platform = $env:PLATFORM
    if (!$Platform) {
        $Platform = "Win32"
    } 

    if ($Platform -eq "Win32") {
        $BuildPlatform = "x86"
    }
    else {
        $BuildPlatform = "x64"
    }
    
    Write-Diagnostic "Initializing EasyHook build environment."
    Write-Diagnostic "EasyHook version = $AssemblyVersion"

    # Download local version of NuGet
    DownloadNuget

    # Restore solution and download pakcages needed
    RestoreNugetPackages

    # Update assembly C# files with correct version  
    WriteAssemblyVersion
    
    $Toolchain = Get-Toolchain $Target
    FindVisualStudio $Toolchain
    
    $output = Get-ProcessOutput -FileName $script:VSWhere -Args '-latest -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe'
    $MSBuildExe = ($output.StandardOutput -split '\n')[0]

    switch -Exact ($Target) {
        "vs2013" {
            $MSBuildToolVersion = "12.0"
        }
        "vs2015" {
            $MSBuildToolVersion = "14.0"
        }
        "vs2017" {
            $MSBuildToolVersion = "14.1"
        }
        "vs2019" {
            $MSBuildToolVersion = "14.2"
        }
    }

    $BatchEnvironment = Join-Path $EasyHookBin "setup_environment.bat"
    Set-Content -Path $BatchEnvironment -Value "" -Force
    Add-Content $BatchEnvironment "set VISUAL_STUDIO_NAME=$Target"
    Add-Content $BatchEnvironment "set VISUAL_STUDIO_PATH=$VSInstallationPath"
    Add-Content $BatchEnvironment "set BUILD_TOOL_VERSION=$Toolchain"
    Add-Content $BatchEnvironment "set EASYHOOK_TOOLS=$ToolsDir"
    Add-Content $BatchEnvironment "set EASYHOOK_ROOT=$EasyHookRoot"
    Add-Content $BatchEnvironment "set CONFIGURATION=$Configuration"
    Add-Content $BatchEnvironment "set PLATFORM=$Platform"
    Add-Content $BatchEnvironment "set BUILD_PLATFORM=$BuildPlatform"
    Add-Content $BatchEnvironment "set MSBUILD=$MSBuildExe"
    Add-Content $BatchEnvironment "set MSBUILD_TOOL_VERSION=$MSBuildToolVersion"

    if ($null -eq $script:VSInstallationPath) {
        Warn "Toolchain $Toolchain is not installed on your development machine, skipping build."
        Return
    }
    
    Write-Diagnostic "Visual Studio Installation Path: $VSInstallationPath"
    Write-Diagnostic "Toolchain: $Toolchain"

    $msiPath = Join-Path $script:EasyHookBin "CoApp.Tools.Powershell.msi"
    
    (New-Object Net.WebClient).DownloadFile('https://easyhook.github.io/downloads/CoApp.Tools.Powershell.msi', $msiPath)
    
    Get-ProcessOutput -FileName "c:\windows\system32\cmd.exe" -Args "/c start /wait msiexec /i ""$msiPath"" /quiet"
    
    # Update environment path
    $env:PSModulePath = $env:PSModulePath + ';C:\Program Files (x86)\Outercurve Foundation\Modules'
    
    # Import CoApp module (for packaging native NuGet)
    Import-Module CoApp
}

if ($Initialize) {
    Initialize-Environment
}
