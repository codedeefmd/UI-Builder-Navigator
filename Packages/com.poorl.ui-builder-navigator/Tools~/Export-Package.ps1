param(
    [string]$OutputPath
)

$ErrorActionPreference = 'Stop'

$pluginRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..')).TrimEnd('\', '/')
# 嵌入式 UPM 包位于工程 Packages 下；导出为 Assets 目录的兼容插件包，保留原 GUID。
$packagesDirectory = [IO.Directory]::GetParent($pluginRoot)
if ($packagesDirectory.Name -ne 'Packages') {
    throw "此脚本须从开发工程 Packages 下的嵌入包运行：$pluginRoot"
}
$projectRoot = $packagesDirectory.Parent.FullName
$assetPrefix = 'Assets/UIBuilderUssNavigator'

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = Join-Path $projectRoot 'Builds\Tools\UIBuilderUssNavigator.unitypackage'
} elseif (-not [IO.Path]::IsPathRooted($OutputPath)) {
    $OutputPath = Join-Path $projectRoot $OutputPath
}
$resolvedOutput = [IO.Path]::GetFullPath($OutputPath)

# Tools~、Tests~ 等以“~”结尾的路径段不会被 Unity 导入，因此也不能进入包。
function Test-UnityAssetPath([string]$fullPath) {
    if ($fullPath.EndsWith('.meta', [StringComparison]::OrdinalIgnoreCase)) {
        return $false
    }

    $relative = [IO.Path]::GetRelativePath($pluginRoot, $fullPath)
    # UPM 的包身份不属于 Assets 插件；兼容导出保留原插件的文件清单和 GUID。
    if ($relative -eq 'package.json') { return $false }
    if ($relative -eq '.') { return $true }
    foreach ($segment in $relative.Split([char[]]@('\', '/'), [StringSplitOptions]::RemoveEmptyEntries)) {
        if ($segment.EndsWith('~', [StringComparison]::Ordinal)) { return $false }
    }
    return $true
}

function Get-AssetGuid([string]$metaPath) {
    if (-not [IO.File]::Exists($metaPath)) {
        throw "缺少 Unity meta 文件：$metaPath"
    }

    $match = [regex]::Match([IO.File]::ReadAllText($metaPath), '(?m)^guid:\s*([0-9a-fA-F]{32})\s*$')
    if (-not $match.Success) {
        throw "meta 中没有有效 GUID：$metaPath"
    }
    return $match.Groups[1].Value.ToLowerInvariant()
}

function Assert-SafeStagingPath([string]$path, [string]$parentPath) {
    $resolvedPath = (Resolve-Path -LiteralPath $path).Path.TrimEnd('\', '/')
    $resolvedParent = (Resolve-Path -LiteralPath $parentPath).Path.TrimEnd('\', '/') + [IO.Path]::DirectorySeparatorChar
    if (-not $resolvedPath.StartsWith($resolvedParent, [StringComparison]::OrdinalIgnoreCase)) {
        throw "拒绝清理专用 staging 目录之外的路径：$resolvedPath"
    }
    return $resolvedPath
}

$stagingParent = Join-Path ([IO.Path]::GetTempPath()) 'UIBuilderUssNavigatorExport'
[IO.Directory]::CreateDirectory($stagingParent) | Out-Null
$stagingRoot = Join-Path $stagingParent ([guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($stagingRoot) | Out-Null

try {
    $items = @((Get-Item -LiteralPath $pluginRoot)) +
        @(Get-ChildItem -LiteralPath $pluginRoot -Recurse -Force | Where-Object {
            Test-UnityAssetPath $_.FullName
        })

    $utf8NoBom = [Text.UTF8Encoding]::new($false)
    $guids = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($item in $items) {
        $metaPath = if ($item.FullName -eq $pluginRoot) { Join-Path $PSScriptRoot 'UnitypackageRoot.meta' } else { $item.FullName + '.meta' }
        $guid = Get-AssetGuid $metaPath
        if (-not $guids.Add($guid)) {
            throw "插件内出现重复 GUID：$guid"
        }

        $guidDirectory = Join-Path $stagingRoot $guid
        [IO.Directory]::CreateDirectory($guidDirectory) | Out-Null
        [IO.File]::Copy($metaPath, (Join-Path $guidDirectory 'asset.meta'))

        $relative = [IO.Path]::GetRelativePath($pluginRoot, $item.FullName).Replace('\', '/')
        $assetPath = if ($relative -eq '.') { $assetPrefix } else { "$assetPrefix/$relative" }
        [IO.File]::WriteAllText((Join-Path $guidDirectory 'pathname'), $assetPath, $utf8NoBom)

        # Unitypackage 中目录只有 asset.meta 和 pathname；普通文件才写入 asset。
        if (-not $item.PSIsContainer) {
            [IO.File]::Copy($item.FullName, (Join-Path $guidDirectory 'asset'))
        }
    }

    $tar = Join-Path $env:SystemRoot 'System32\tar.exe'
    if (-not [IO.File]::Exists($tar)) {
        throw "找不到 Windows 自带 tar：$tar"
    }
    $archive = Join-Path $stagingRoot 'UIBuilderUssNavigator.unitypackage'
    $guidArguments = @($guids | Sort-Object)
    & $tar -czf $archive -C $stagingRoot @guidArguments
    if ($LASTEXITCODE -ne 0) {
        throw "tar 创建 unitypackage 失败，退出码：$LASTEXITCODE"
    }

    $outputDirectory = [IO.Path]::GetDirectoryName($resolvedOutput)
    [IO.Directory]::CreateDirectory($outputDirectory) | Out-Null
    [IO.File]::Copy($archive, $resolvedOutput, $true)
    Write-Host "已导出 $($guids.Count) 个 Unity 资产：$resolvedOutput"
}
finally {
    if ([IO.Directory]::Exists($stagingRoot)) {
        $safeStagingRoot = Assert-SafeStagingPath $stagingRoot $stagingParent
        [IO.Directory]::Delete($safeStagingRoot, $true)
    }
}
