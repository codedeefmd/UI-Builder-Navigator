$ErrorActionPreference = 'Stop'

$testsDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path
$pluginDirectory = Split-Path -Parent $testsDirectory
$locatorPath = Join-Path $pluginDirectory 'Editor\UssRuleLocator.cs'
$harnessPath = Join-Path $testsDirectory 'UssRuleLocatorTests.cs.txt'

$locatorSource = Get-Content -LiteralPath $locatorPath -Raw
$harnessSource = Get-Content -LiteralPath $harnessPath -Raw

Add-Type -TypeDefinition ($locatorSource + [Environment]::NewLine + $harnessSource) -Language CSharp

$result = [UIBuilderUssNavigator.UssRuleLocatorTestHarness]::Run()
Write-Host $result

if (-not $result.StartsWith('PASS ', [StringComparison]::Ordinal)) {
    throw "Unexpected test result: $result"
}
