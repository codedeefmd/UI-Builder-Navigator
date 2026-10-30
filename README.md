# UI Builder Navigator

[English](README.en.md) | 中文

用于 Unity 6 UI Builder 的 Editor 扩展，为 UXML/USS 预览、样式定位和布局诊断提供辅助功能。该仓库是从 SEC Lab 工程中独立迁出的开发工程；嵌入包不依赖 SEC Lab。

## 当前范围

- 包名：`com.poorl.ui-builder-navigator`，版本：`0.1.0`
- Editor 程序集：`UIBuilderUssNavigator.Editor`
- 菜单：`Window > UI Toolkit > USS Preview Navigator`
- 插件界面支持中文与英文；菜单项按当前语言显示为“启用 / 语言”或“Enabled / Language”
- 当前阶段不包含 Demo

## 仓库结构

```text
Assets/                                      Unity 开发工程的场景与默认资源
Packages/com.poorl.ui-builder-navigator/     嵌入式 UPM 包源码、文档与测试
Packages/manifest.json                       开发工程的包清单
ProjectSettings/                             Unity 开发工程设置
```

使用 Unity Hub 以 **Unity 6000.3.27f1** 打开仓库根目录。Unity 会通过 `Packages/manifest.json` 自动加载嵌入包，无需手动复制插件文件。开发工程使用 Built-In Render Pipeline，并保留空的 `SampleScene` 与默认 Input System 资源。

## 安装到其他 Unity 项目

远端仓库尚未创建。创建远端后，请将下列占位地址中的 `<GITHUB_USER>/<REPOSITORY>` 替换为实际仓库路径，再通过 Package Manager 的 **Add package from git URL** 安装：

```text
https://github.com/<GITHUB_USER>/<REPOSITORY>.git?path=/Packages/com.poorl.ui-builder-navigator
```

也可以将同一地址写入目标项目的 `Packages/manifest.json`：

```json
"com.poorl.ui-builder-navigator": "https://github.com/<GITHUB_USER>/<REPOSITORY>.git?path=/Packages/com.poorl.ui-builder-navigator"
```

## 测试

包内 `Tests~` 目录包含 7 个独立 PowerShell 测试脚本：

- `Run-CodePreviewSearchTests.ps1`
- `Run-DiagnosticTextTests.ps1`
- `Run-LocalizationTests.ps1`
- `Run-UssRuleLocatorTests.ps1`
- `Run-UssSyntaxHighlighterTests.ps1`
- `Run-UxmlElementLocatorTests.ps1`
- `Run-UxmlSyntaxHighlighterTests.ps1`

在仓库根目录使用 PowerShell 7 运行全部脚本；每个脚本都会启动一个独立的 `pwsh` 进程：

```powershell
Get-ChildItem -LiteralPath 'Packages/com.poorl.ui-builder-navigator/Tests~' -Filter 'Run-*Tests.ps1' |
    Sort-Object Name |
    ForEach-Object { pwsh -NoProfile -File $_.FullName }
```

## 兼容性

插件会使用 Unity UI Builder 的内部 Editor 接口。目前仅确认兼容 **Unity 6000.3.27f1（Windows Editor）**；其他 Unity 版本尚未验证。

## 许可证

本项目尚未指定开源许可证。
