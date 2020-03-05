param(
    [ValidateSet("vs2013", "vs2015", "vs2017", "vs2019", "nupkg-only")]
    [Parameter(Position = 0)] 
    [string] $Target = "vs2017",
    [Parameter(Position = 1)]
    [string] $AssemblyVersion = "2.8.0.0" 
)

$WorkingDir = split-path -parent $MyInvocation.MyCommand.Definition
$EasyHookSln = Join-Path $WorkingDir 'EasyHook.sln'
$VSInstallationPath = $null

function Write-Diagnostic 
{
    param(
        [Parameter(Position = 0, Mandatory = $true, ValueFromPipeline = $true)]
        [string] $Message
    )

    Write-Host
    Write-Host $Message -ForegroundColor Green
    Write-Host
}

function DownloadNuget()
{
    $Nuget = Join-Path $env:LOCALAPPDATA .\nuget\NuGet.exe
    if(-not (Test-Path $Nuget))
    {
        $Client = New-Object System.Net.WebClient;
        $Client.DownloadFile('http://nuget.org/nuget.exe', $Nuget);
    }
}

function RestoreNugetPackages()
{
    $Nuget = Join-Path $env:LOCALAPPDATA .\nuget\NuGet.exe
    Get-Location
    . $Nuget restore -OutputDirectory .\Packages EasyHook.sln

    . $Nuget install -OutputDirectory .\Packages MSBuildTasks -Version 1.5.0.196
    
    . $Nuget install -OutputDirectory .\Packages vswhere -Version 2.8.4
}

# https://github.com/jbake/Powershell_scripts/blob/master/Invoke-BatchFile.ps1
function Invoke-BatchFile 
{
   param(
        [Parameter(Position = 0, Mandatory = $true, ValueFromPipeline = $true)]
        [string]$Path, 
        [Parameter(Position = 1, Mandatory = $true, ValueFromPipeline = $true)]
        [string]$Parameters
   )

   $tempFile = [IO.Path]::GetTempFileName()  

   cmd.exe /c " `"$Path`" $Parameters && set > `"$tempFile`" " 

   Get-Content $tempFile | Foreach-Object {   
       if ($_ -match "^(.*?)=(.*)$")  
       { 
           Set-Content "env:\$($matches[1])" $matches[2]  
       } 
   }  

   Remove-Item $tempFile
}

function Die 
{
    param(
        [Parameter(Position = 0, ValueFromPipeline = $true)]
        [string] $Message
    )

    Write-Host
    Write-Error $Message 
    exit 1
}

function Warn 
{
    param(
        [Parameter(Position = 0, ValueFromPipeline = $true)]
        [string] $Message
    )

    Write-Host
    Write-Host $Message -ForegroundColor Yellow
    Write-Host
}

function TernaryReturn 
{
    param(
        [Parameter(Position = 0, ValueFromPipeline = $true)]
        [bool] $Yes,
        [Parameter(Position = 1, ValueFromPipeline = $true)]
        $Value,
        [Parameter(Position = 2, ValueFromPipeline = $true)]
        $Value2
    )

    if($Yes) {
        return $Value
    }
    
    $Value2
}

function Msvs 
{
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

    $VSWhere = ".\Packages\vswhere.2.8.4\tools\vswhere.exe"
    $MSBuildExe = . $VSWhere -latest -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe | select-object -first 1

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
    }

    if ($null -eq $VXXCommonTools -or (-not (Test-Path($VXXCommonTools)))) {
        Die 'Error unable to find any visual studio environment'
    }

    $VCVarsAll = Join-Path $VXXCommonTools vcvarsall.bat
    if (-not (Test-Path $VCVarsAll)) {
        Die "Unable to find $VCVarsAll"
    }

    # Only configure build environment once
    if($null -eq $env:EASYHOOK_BUILD_IS_BOOTSTRAPPED) {
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

    if($Process.ExitCode -ne 0)
    {
        Write-Host "stdout: $stdout"
        Write-Host "stderr: $stderr"
        Die "Build failed"
    }
}

function FindVS
{
    param(
        [Parameter(Position = 0, ValueFromPipeline = $true)]
        [string] $Toolchain
    )

    $VSWhere = ".\Packages\vswhere.2.8.4\tools\vswhere.exe"

    # use vswhere to locate toolchain dir


    if ($Toolchain -eq 'v120') {
        $script:VSInstallationPath = . $VSWhere -legacy -version "[12.0,13.0)" -property installationPath
    }
    
    if ($Toolchain -eq 'v140') {
        $script:VSInstallationPath = . $VSWhere -legacy -version "[14.0,15.0)" -property installationPath
    }
    
    if ($Toolchain -eq 'v141') {
        $script:VSInstallationPath = . $VSWhere -version "[15.0,16.0)" -property installationPath
    }
    
    if ($Toolchain -eq 'v142') {
        $script:VSInstallationPath = . $VSWhere -version "[15.0,16.0)" -property installationPath
    }
}

function VSX 
{
    param(
        [ValidateSet('v120', 'v140', 'v141')]
        [Parameter(Position = 0, ValueFromPipeline = $true)]
        [string] $Toolchain
    )

    FindVS "$Toolchain"
    
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

function WriteAssemblyVersionForFile
{
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

function WriteAssemblyVersion
{
    param()

    $Filename = Join-Path $WorkingDir EasyHook\Properties\AssemblyInfo.cs
    Write-Diagnostic "$Filename"
    WriteAssemblyVersionForFile "$Filename"
    $Filename = Join-Path $WorkingDir EasyLoad\Properties\AssemblyInfo.cs
    WriteAssemblyVersionForFile "$Filename"
}

function Nupkg
{
}

Write-Diagnostic "EasyHook version = $AssemblyVersion"

DownloadNuget

RestoreNugetPackages

WriteAssemblyVersion

switch -Exact ($Target)
{
    "nupkg-only"
    {
        Nupkg
    }
    "vs2013"
    {
        VSX v120
        Nupkg
    }
    "vs2015"
    {
        VSX v140
        Nupkg
    }
    "vs2017"
    {
        VSX v141
        Nupkg
    }
    "vs2019"
    {
        VSX v142
        Nupkg
    }
}