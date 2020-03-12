
$script:ToolsDir = split-path -parent $MyInvocation.MyCommand.Definition
$script:EasyHookRootDir = (Get-Item $ToolsDir).Parent
$script:ClangFormat = [IO.Path]::Combine(
    $EasyHookRootDir.FullName, 'node_modules', 'clang-format', 'bin', 'win32', 'clang-format.exe')

$script:SetupScript = Join-Path $script:ToolsDir 'setup.ps1'

$script:EditorConfigLint = [IO.Path]::Combine(
    $EasyHookRootDir.FullName, 'node_modules', '.bin', 'eclint.cmd')

. $script:SetupScript -Target $Target

function Run-Clang-Format-Directory() {
    param(
        [Parameter(Position = 0, Mandatory = $true, ValueFromPipeline = $true)]
        [string]$Root
    )

    $config = Join-Path $EasyHookRootDir.FullName '.clang-format'

    $files = Get-ChildItem -Path $Root -File
    foreach ($file in $files) {
        $type = ""

        if ($file.Extension -eq ".cpp" -or
            $file.Extension -eq ".c" -or
            $file.Extension -eq ".h") {
            $type = "Chromium"
        }

        if ($file.Extension -eq ".cs") {
            $type = "Microsoft"
        }

        if ($file.Name -like "*AssemblyInfo*") {
            $type = ""
        }

        if ($file.Name -like "*.Designer.cs") {
            $type = ""
        }

        if (![string]::IsNullOrEmpty($type)) {
            $output = Get-ProcessOutput -FileName $script:ClangFormat -Args (
                "-i " + $file.FullName)

            Write-Diagnostic ("clang-format " + $file.FullName)

            if (![string]::IsNullOrEmpty($output.StandardOutput)) {
                Write-Diagnostic ("clang-format : Info : " + $output.StandardOutput)
            }

            if (![string]::IsNullOrEmpty($output.StandardError)) {
                Write-Diagnostic ("clang-format : Error : " + $output.StandardError)
            }
        }
    }

    $children = Get-ChildItem -Path $Root -Directory

    foreach ($child in $children) {
        Run-Clang-Format-Directory $child.FullName
    }
}

Write-Diagnostic "eclint: Started."
$_ = Invoke-BatchFile -Path $script:EditorConfigLint -Parameters "fix **/*.cs **/*.cpp **/*.h **/*.c **/*.ps1 **/*.yml **/*.bat"
Write-Diagnostic "eclint: Done."

Run-Clang-Format-Directory $script:EasyHookRootDir.FullName
Write-Diagnostic "clang-format: Done."
