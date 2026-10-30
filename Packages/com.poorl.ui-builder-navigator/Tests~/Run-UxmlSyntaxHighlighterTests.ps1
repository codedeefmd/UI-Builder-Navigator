$ErrorActionPreference = 'Stop'

$testsDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path
$pluginDirectory = Split-Path -Parent $testsDirectory
$sourcePath = Join-Path $pluginDirectory 'Editor\UxmlSyntaxHighlighter.cs'
$sharedPath = Join-Path $pluginDirectory 'Editor\SyntaxText.cs'
$harnessPath = Join-Path $testsDirectory 'UxmlSyntaxHighlighterTests.cs.txt'

$source = Get-Content -LiteralPath $sourcePath -Raw
$shared = Get-Content -LiteralPath $sharedPath -Raw
$harness = Get-Content -LiteralPath $harnessPath -Raw
Add-Type -TypeDefinition ($source + [Environment]::NewLine + $shared + [Environment]::NewLine + $harness) -Language CSharp

$result = [UIBuilderUssNavigator.UxmlSyntaxHighlighterTestHarness]::Run()
Write-Host $result
if (-not $result.StartsWith('PASS ', [StringComparison]::Ordinal)) {
    throw "Unexpected test result: $result"
}
