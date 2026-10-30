# UI Builder Navigator

[English](README.en.md) | 中文

用于 Unity 6 UI Builder 的 Editor 扩展，为 UXML/USS 预览、样式定位和布局诊断提供辅助功能。可作为独立 UPM 包安装到其他 Unity 项目。

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

打开 Unity 的 **Package Manager**，点击 **+ → Add package from git URL**，粘贴以下地址并确认：

```text
https://github.com/codedeefmd/UI-Builder-Navigator.git?path=/Packages/com.poorl.ui-builder-navigator
```

或者将下面这一项加入目标项目 `Packages/manifest.json` 现有的 `dependencies` 对象中：

```json
"com.poorl.ui-builder-navigator": "https://github.com/codedeefmd/UI-Builder-Navigator.git?path=/Packages/com.poorl.ui-builder-navigator"
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

本项目采用 [MIT 许可证](LICENSE)。允许商用、修改和再分发，使用或分发时须保留版权及许可证声明。UPM 包内也包含 [许可证副本](Packages/com.poorl.ui-builder-navigator/LICENSE.md)。
