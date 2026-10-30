# UI Builder Navigator

English | [中文](README.md)

An Editor extension for Unity 6 UI Builder that assists with UXML/USS previews, style navigation, and layout diagnostics. Install it in other Unity projects as an independent UPM package.

## Current scope

- Package: `com.poorl.ui-builder-navigator`, version `0.2.0`
- Editor assembly: `UIBuilderUssNavigator.Editor`
- Menu: `Window > UI Toolkit > USS Preview Navigator`
- The plugin UI and Learning Lab examples support Chinese and English

## Repository structure

```text
Assets/                                      Scenes and default assets for the Unity development project
Packages/com.poorl.ui-builder-navigator/     Embedded UPM package source, documentation, and tests
Packages/manifest.json                       Development project package manifest
ProjectSettings/                             Unity development project settings
```

Open the repository root in Unity Hub with **Unity 6000.3.27f1**. Unity loads the embedded package automatically through `Packages/manifest.json`; no manual plugin copy is required. The development project uses the Built-In Render Pipeline and retains the empty `SampleScene` and default Input System assets.

## Learning Lab examples

Three UXML/USS-only examples live under `Packages/com.poorl.ui-builder-navigator/Samples~/LearningLab`. Follow them in order:

1. **01 · Elements & Code Navigation**: navigate from the canvas or Hierarchy to UXML tags and related USS rules.
2. **02 · Style Sources & Cascade**: inspect shared styles, combined selectors, inheritance, inline styles, and cascade results.
3. **03 · Parent Layout & Box Model**: compare parent-child bounds, Flex values, percentage sizes, and ScrollView layout parents.

Each page has a screenshot walkthrough and steps to follow: [English illustrated guide](Packages/com.poorl.ui-builder-navigator/Samples~/LearningLab/README.en.md) / [中文图文教程](Packages/com.poorl.ui-builder-navigator/Samples~/LearningLab/README.md).

Open `Window > UI Toolkit > USS Preview Navigator > Open Examples` and choose a page. The menu opens the Chinese or English page according to the plugin's current language. On first use, Unity's native Sample API imports Learning Lab to `Assets/Samples/UI Builder Navigator/0.2.0/Learning Lab`; later opens reuse the imported content without overwriting practice edits.

The first menu-driven open of a page initializes a `1280 × 900` canvas, expands a collapsed code area, and fits the viewport. Existing canvas settings are preserved. Importing the Sample through Package Manager and double-clicking a UXML uses UI Builder's native defaults and does not perform this menu initialization. The examples are for Editor learning and do not represent Player behavior.

This screenshot shows the first example in Unity 6000.3.27f1 with the English sample page and English diagnostics. The selected button stays linked to its UXML tag, USS rule, and Inspector diagnostics.

![Elements and code navigation example](Packages/com.poorl.ui-builder-navigator/Samples~/LearningLab/Documentation/Images/learning-lab-navigation-en.png)

More Editor screenshots: [inheritance, inline styles, and alpha colors](Packages/com.poorl.ui-builder-navigator/Samples~/LearningLab/Documentation/Images/learning-lab-cascade-en.png), [parent-child layout comparison and canvas bounds](Packages/com.poorl.ui-builder-navigator/Samples~/LearningLab/Documentation/Images/learning-lab-layout-en.png).

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

The package contains eight independent PowerShell test runners under `Tests~`:

- `Run-CodePreviewSearchTests.ps1`
- `Run-DiagnosticTextTests.ps1`
- `Run-ExampleAssetsTests.ps1`
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
