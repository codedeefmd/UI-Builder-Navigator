$ErrorActionPreference = 'Stop'
$packageRoot = Split-Path -Parent $PSScriptRoot
$sampleRoot = Join-Path $packageRoot 'Samples~/LearningLab'
$script:assertions = 0
function Check([bool]$condition, [string]$message) {
    $script:assertions++
    if (-not $condition) { throw $message }
}

# 检查可分发的源资产，而非 Assets/Samples 中可被用户修改的练习副本。
$manifest = Get-Content -LiteralPath (Join-Path $packageRoot 'package.json') -Raw | ConvertFrom-Json
Check ($manifest.samples.Count -eq 1) 'Learning Lab sample registration is missing or duplicated.'
Check ($manifest.samples[0].path -eq 'Samples~/LearningLab') 'Sample source path is inconsistent.'
Check ((Get-Content -LiteralPath (Join-Path $sampleRoot 'LICENSE.md') -Raw) -eq
    (Get-Content -LiteralPath (Join-Path $packageRoot 'LICENSE.md') -Raw)) 'Sample license differs from the package license.'
$pages = @(
    @{ Path = '01-Navigation/Navigation'; Names = @('select-me-button', 'demo-name-field', 'demo-enabled-toggle', 'demo-scroll', 'template-card-instance') },
    @{ Path = '02-Cascade/Cascade'; Names = @('cascade-base-button', 'cascade-primary-button', 'cascade-inline-button', 'cascade-inherited-label', 'cascade-alpha-swatch') },
    @{ Path = '03-Layout/Layout'; Names = @('layout-stretch-button', 'layout-center-button', 'layout-percent-child', 'layout-flex-child', 'layout-scroll') }
)
foreach ($page in $pages) {
    $signatures = @{}
    foreach ($language in @('zh', 'en')) {
        $path = Join-Path $sampleRoot ($page.Path + '.' + $language + '.uxml')
        Check (Test-Path -LiteralPath $path) "Missing $path"
        [xml]$document = Get-Content -LiteralPath $path -Raw
        $nodes = @($document.SelectNodes('//*'))
        $signatures[$language] = @($nodes | ForEach-Object {
            $_.LocalName + '|' + $_.GetAttribute('name') + '|' + $_.GetAttribute('class')
        }) -join "`n"
        foreach ($name in $page.Names) {
            Check (@($nodes | Where-Object { $_.GetAttribute('name') -eq $name }).Count -eq 1) "Missing/duplicate $name in $path"
        }
    }
    Check ($signatures.zh -ceq $signatures.en) "Bilingual structure differs: $($page.Path)"
}
foreach ($file in Get-ChildItem -LiteralPath $sampleRoot -Filter '*.uxml' -Recurse) {
    [xml]$document = Get-Content -LiteralPath $file.FullName -Raw
    foreach ($reference in $document.SelectNodes('//*[@src]')) {
        $source = $reference.GetAttribute('src')
        Check (-not [IO.Path]::IsPathRooted($source) -and $source -notmatch '^[a-z]+://') "Nonportable reference in $($file.Name): $source"
        Check (Test-Path -LiteralPath (Join-Path $file.DirectoryName $source)) "Missing reference in $($file.Name): $source"
    }
    Check ((Get-Content -LiteralPath $file.FullName -Raw) -notmatch 'SecLab|PlatformViewport|project://database') "Business-project dependency in $($file.Name)"
}
Check (@(Get-ChildItem -LiteralPath $sampleRoot -Filter '*.cs' -Recurse).Count -eq 0) 'Fixed sample UI must remain in UXML/USS.'
Write-Host "PASS $script:assertions assertions"
