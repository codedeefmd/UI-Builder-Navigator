# UI Builder Navigator

English | [中文](README.md)

An Editor extension for Unity 6 UI Builder that assists with UXML/USS previews, style navigation, and layout diagnostics. Install it in other Unity projects as an independent UPM package.

## Current scope

- Package: `com.poorl.ui-builder-navigator`, version `0.1.0`
- Editor assembly: `UIBuilderUssNavigator.Editor`
- Menu: `Window > UI Toolkit > USS Preview Navigator`
- The plugin UI supports Chinese and English; menu items appear as `启用 / 语言` or `Enabled / Language`
- No demo is included at this stage

## Repository structure

```text
Assets/                                      Scenes and default assets for the Unity development project
Packages/com.poorl.ui-builder-navigator/     Embedded UPM package source, documentation, and tests
Packages/manifest.json                       Development project package manifest
ProjectSettings/                             Unity development project settings
```

Open the repository root in Unity Hub with **Unity 6000.3.27f1**. Unity loads the embedded package automatically through `Packages/manifest.json`; no manual plugin copy is required. The development project uses the Built-In Render Pipeline and retains the empty `SampleScene` and default Input System assets.

## Install in another Unity project

Open Unity's **Package Manager**, choose **+ → Add package from git URL**, and paste this address:

```text
https://github.com/codedeefmd/UI-Builder-Navigator.git?path=/Packages/com.poorl.ui-builder-navigator
```

Alternatively, add this entry to the existing `dependencies` object in the target project's `Packages/manifest.json`:

```json
"com.poorl.ui-builder-navigator": "https://github.com/codedeefmd/UI-Builder-Navigator.git?path=/Packages/com.poorl.ui-builder-navigator"
```

## Tests

The package contains seven independent PowerShell test runners under `Tests~`:

- `Run-CodePreviewSearchTests.ps1`
- `Run-DiagnosticTextTests.ps1`
- `Run-LocalizationTests.ps1`
- `Run-UssRuleLocatorTests.ps1`
- `Run-UssSyntaxHighlighterTests.ps1`
- `Run-UxmlElementLocatorTests.ps1`
- `Run-UxmlSyntaxHighlighterTests.ps1`

Run all scripts from the repository root with PowerShell 7. Each script is launched in a separate `pwsh` process:

```powershell
Get-ChildItem -LiteralPath 'Packages/com.poorl.ui-builder-navigator/Tests~' -Filter 'Run-*Tests.ps1' |
    Sort-Object Name |
    ForEach-Object { pwsh -NoProfile -File $_.FullName }
```

## Compatibility

The extension uses internal Unity UI Builder Editor APIs. Compatibility is currently confirmed only for **Unity 6000.3.27f1 on Windows Editor**; other Unity versions have not been verified.

## License

Licensed under the [MIT License](LICENSE). Commercial use, modification and redistribution are permitted with the copyright and license notices preserved. The UPM package includes its own [license copy](Packages/com.poorl.ui-builder-navigator/LICENSE.md).
