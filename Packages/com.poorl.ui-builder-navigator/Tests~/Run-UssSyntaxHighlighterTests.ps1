$ErrorActionPreference = 'Stop'

$testsDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path
$pluginDirectory = Split-Path -Parent $testsDirectory
$sharedPath = Join-Path $pluginDirectory 'Editor\SyntaxText.cs'
$sourcePath = Join-Path $pluginDirectory 'Editor\UssSyntaxHighlighter.cs'
$harnessPath = Join-Path $testsDirectory 'UssSyntaxHighlighterTests.cs.txt'

$shared = Get-Content -LiteralPath $sharedPath -Raw
$source = Get-Content -LiteralPath $sourcePath -Raw
$harness = Get-Content -LiteralPath $harnessPath -Raw
Add-Type -TypeDefinition ($source + [Environment]::NewLine + $shared + [Environment]::NewLine + $harness) -Language CSharp

$result = [UIBuilderUssNavigator.UssSyntaxHighlighterTestHarness]::Run()
Write-Host $result
if (-not $result.StartsWith('PASS ', [StringComparison]::Ordinal)) {
    throw "Unexpected test result: $result"
}
