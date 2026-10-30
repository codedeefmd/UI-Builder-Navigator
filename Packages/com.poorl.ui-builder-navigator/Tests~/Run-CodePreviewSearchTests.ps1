$ErrorActionPreference = 'Stop'

$testsDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path
$pluginDirectory = Split-Path -Parent $testsDirectory
$searchPath = Join-Path $pluginDirectory 'Editor\CodePreviewSearch.cs'
$sharedPath = Join-Path $pluginDirectory 'Editor\SyntaxText.cs'
$harnessPath = Join-Path $testsDirectory 'CodePreviewSearchTests.cs.txt'

$search = Get-Content -LiteralPath $searchPath -Raw
$shared = Get-Content -LiteralPath $sharedPath -Raw
$harness = Get-Content -LiteralPath $harnessPath -Raw
Add-Type -TypeDefinition ($search + [Environment]::NewLine + $shared + [Environment]::NewLine + $harness) -Language CSharp

$result = [UIBuilderUssNavigator.CodePreviewSearchTestHarness]::Run()
Write-Host $result
if (-not $result.StartsWith('PASS ', [StringComparison]::Ordinal)) {
    throw "Unexpected test result: $result"
}
