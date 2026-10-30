$ErrorActionPreference = 'Stop'

$testsDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path
$pluginDirectory = Split-Path -Parent $testsDirectory
$locatorPath = Join-Path $pluginDirectory 'Editor\UxmlElementLocator.cs'
$rangeSourcePath = Join-Path $pluginDirectory 'Editor\UssRuleLocator.cs'
$harnessPath = Join-Path $testsDirectory 'UxmlElementLocatorTests.cs.txt'

$locatorSource = Get-Content -LiteralPath $locatorPath -Raw
$rangeSource = Get-Content -LiteralPath $rangeSourcePath -Raw
$harnessSource = Get-Content -LiteralPath $harnessPath -Raw
# 合并真实源码而不是复制 RuleRange 替身；文件级 using 统一放到编译单元最前面。
$sources = $locatorSource + [Environment]::NewLine + $rangeSource + [Environment]::NewLine + $harnessSource
$usingPattern = '(?m)^using [^\r\n]+;\r?\n'
$usings = ([regex]::Matches($sources, $usingPattern).Value | Select-Object -Unique) -join ''
Add-Type -TypeDefinition ($usings + [regex]::Replace($sources, $usingPattern, '')) -Language CSharp

$result = [UIBuilderUssNavigator.UxmlElementLocatorTestHarness]::Run()
Write-Host $result
if (-not $result.StartsWith('PASS ', [StringComparison]::Ordinal)) {
    throw "Unexpected test result: $result"
}
