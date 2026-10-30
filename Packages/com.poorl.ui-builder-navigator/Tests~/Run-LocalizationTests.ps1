$ErrorActionPreference = 'Stop'

$testDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path
$pluginDirectory = Split-Path -Parent $testDirectory
$source = Get-Content -LiteralPath (Join-Path $pluginDirectory 'Editor/L10n.cs') -Raw
$harness = Get-Content -LiteralPath (Join-Path $testDirectory 'LocalizationTests.cs.txt') -Raw
# 只替代语言设置的两个 Unity 环境依赖，不加载 Unity、不写真实 EditorPrefs。
Add-Type -TypeDefinition ($source + [Environment]::NewLine + $harness) -Language CSharp
$result = [UIBuilderUssNavigator.LocalizationTestHarness]::Run()
Write-Host $result
if (-not $result.StartsWith('PASS ', [StringComparison]::Ordinal)) { throw $result }
