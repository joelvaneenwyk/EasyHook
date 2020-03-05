Write-Host "Running PowerShell install script."

$msiPath = "$($env:USERPROFILE)\CoApp.Tools.Powershell.msi"

(New-Object Net.WebClient).DownloadFile('https://easyhook.github.io/downloads/CoApp.Tools.Powershell.msi', $msiPath)

cmd /c start /wait msiexec /i "$msiPath" /quiet

# Update environment path

$env:PSModulePath = $env:PSModulePath + ';C:\Program Files (x86)\Outercurve Foundation\Modules'

# Import CoApp module (for packaging native NuGet)

Import-Module CoApp