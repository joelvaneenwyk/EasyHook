param(
    [ValidateSet("vs2013", "vs2015", "vs2017", "vs2019", "vs2022", "nupkg-only")]
    [Parameter(Position = 0)]
    [string] $Target = $null,
    [Parameter(Position = 1)]
    [string] $AssemblyVersion = $null
)

$ToolsDir = split-path -parent $MyInvocation.MyCommand.Definition
$SetupScript = Join-Path $ToolsDir 'setup.ps1'

$ToolchainInfo = . $SetupScript -Target $Target

Invoke-TargetBuild $ToolchainInfo
