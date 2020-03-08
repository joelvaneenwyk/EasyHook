Write-Host "Running PowerShell install script."

Function Add-PathVariable {
    param (
        [string]$addPath
    )
    if (Test-Path $addPath){
        $regexAddPath = [regex]::Escape($addPath)
        $arrPath = $env:Path -split ';' | Where-Object {$_ -notMatch 
"^$regexAddPath\\?"}
        $env:Path = ($arrPath + $addPath) -join ';'
    } else {
        Throw "'$addPath' is not a valid path."
    }
}

$dir = [string](Get-Location)

New-Item -ItemType Directory -Force -Path $PSScriptRoot\..\Bin\

$sourceNugetExe = "https://dist.nuget.org/win-x86-commandline/latest/nuget.exe"
$targetNugetExe = "$PSScriptRoot\..\Bin\nuget.exe"
Invoke-WebRequest $sourceNugetExe -OutFile $targetNugetExe
Set-Alias nuget $targetNugetExe -Scope Global -Verbose

nuget restore "$PSScriptRoot\..\EasyHook.sln"

nuget install -OutputDirectory $PSScriptRoot\..\Packages Microsoft.TestPlatform -Version 16.5.0
nuget install -OutputDirectory $PSScriptRoot\..\Packages vswhere -Version 2.8.4
nuget install -OutputDirectory $PSScriptRoot\..\Packages MSBuildTasks -Version 1.5.0.196
nuget install -OutputDirectory $PSScriptRoot\..\Packages Appveyor.TestLogger -Version 2.0.0

$msiPath = "$($env:USERPROFILE)\CoApp.Tools.Powershell.msi"

(New-Object Net.WebClient).DownloadFile('https://easyhook.github.io/downloads/CoApp.Tools.Powershell.msi', $msiPath)

cmd /c start /wait msiexec /i "$msiPath" /quiet

# Update environment path
$env:PSModulePath = $env:PSModulePath + ';C:\Program Files (x86)\Outercurve Foundation\Modules'

# Import CoApp module (for packaging native NuGet)
Import-Module CoApp
