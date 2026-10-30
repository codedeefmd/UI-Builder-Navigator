# UI Builder Navigator

English | [中文](README.md)

An independent Editor extension for Unity UI Builder. It adds readable, linked UXML/USS source previews and a separate Inspector diagnostics page. The development project embeds the UPM package at `Packages/com.poorl.ui-builder-navigator`. Other projects can install its local `package.json` or use a Git URL with this package subfolder. There are no third-party dependencies.

Tested with **Unity 6.3 LTS / 6000.3.27f1 on Windows**. The assembly is Editor-only and does not enter Player builds.

## Installation

In Unity Package Manager, choose **+ → Add package from git URL** and paste:

```text
https://github.com/codedeefmd/UI-Builder-Navigator.git?path=/Packages/com.poorl.ui-builder-navigator
```

Repository: [UI Builder Navigator](https://github.com/codedeefmd/UI-Builder-Navigator).

## Language and settings

Use the **中文 / English** menu at the right of the Inspector tab bar. Changes apply to existing controls immediately. The first launch follows the system language: Chinese systems use `zh`; other systems use `en`. Your subsequent choice is stored separately in EditorPrefs and survives extension and Editor restarts.

You can also use **Window → UI Toolkit → USS Preview Navigator → Language**. This menu remains available while the extension is disabled. The **Enabled** entry and language submenu are translated when you change languages.

Localization covers extension tabs, diagnostics, search and copy menus, related-style navigation, tooltips, empty states and integration errors. Source code, CSS property names and keywords, selectors, filenames and element identifiers remain verbatim. Unity's own Inspector controls, menus and built-in preview titles follow Unity's language settings.

## Learning Lab examples

`Samples~/LearningLab` contains three bilingual UXML/USS examples with no attached C# behavior:

1. **01 · Elements & Code Navigation**: practice UXML tag navigation, related styles, and template source ownership.
2. **02 · Style Sources & Cascade**: inspect shared styles, combined selectors, inheritance, inline styles, and cascade results.
3. **03 · Parent Layout & Box Model**: compare parent-child bounds, Flex values, percentage sizes, and the effective ScrollView layout parent.

Choose a page under **Window → UI Toolkit → USS Preview Navigator → Open Examples**. The three entries are **01 · Elements & Code Navigation**, **02 · Style Sources & Cascade**, and **03 · Parent Layout & Box Model**. The launcher follows the plugin language, opening `.en.uxml` in English or `.zh.uxml` in Chinese.

On first use, Unity's native Sample API imports Learning Lab to `Assets/Samples/UI Builder Navigator/0.2.0/Learning Lab`. Later menu opens reuse the imported content without overwriting practice edits. The first menu-driven open of each page initializes a `1280 × 900` canvas, expands a collapsed code area, and fits the viewport; existing canvas settings remain unchanged. Importing the Sample through Package Manager and double-clicking a UXML uses UI Builder's native defaults instead, without the menu initialization. These examples are for Editor learning and do not represent Player behavior.

## Features

Learning Lab was checked in this standalone project with Unity 6000.3.27f1 on Windows: all three pages open in both languages; styled teaching elements stay horizontally within a 350px canvas; element/rule navigation retains both source locations; cascade, inheritance, inline, alpha variables, and parent-child computed values match the instructions. Reopening preserves imported edits and existing canvas settings. A cross-version import check preserved the earlier exercise files. These checks do not establish Player or other Unity-version compatibility.

`Tests~/LearningLabChecks.cs.txt` and `Tests~/LearningLabViewportChecks.cs.txt` are method bodies for Unity MCP `execute_code`. Open an example and wait for layout before running them. The latter checks the current canvas width; set Canvas Size to 350px for the narrow-layout check. Run these separately from the standalone PowerShell tests.

| Area | Behavior |
| --- | --- |
| UXML and USS previews | Syntax colors, line numbers and virtualized access to the complete in-memory preview. |
| Element navigation | Select a Hierarchy or canvas element to reveal its UXML opening tag. The location remains while you inspect related styles. |
| Rule navigation | Select a StyleSheets rule to reveal its USS block. A later click or loss of Builder focus clears the temporary USS location background. |
| Related styles | Right-click a page element, open **Related Styles**, and choose a rule. The style list and USS preview navigate while preserving the element context. |
| Search | Each preview has its own case-insensitive literal search, result count and highlights. Use Enter/Shift+Enter or arrows to cycle, Esc to clear, and Ctrl+F (Cmd+F on macOS) to focus. |
| Copy | Copy selected text, the located opening tag/rule, or the full raw preview. Rich-text color/search markup is never copied. |
| Properties tab | Preserves Unity's native property/style editor and its scrolling area. |
| Diagnostics tab | A single vertical scroll area containing the computed box model, parent/current layout comparison and style sources. |
| Box model | Computed margin, border, padding and content dimensions. Hover a layer to inspect its canvas bounds. |
| Layout comparison | Compare the actual layout parent's content box with the current element's border box. Hover the table for purple/blue canvas outlines. |
| Style sources | Computed values, winning declarations, inherited sources, inline declarations and expandable overridden chains for 33 common properties. Click navigable sources to locate USS. |
| Read-only colors | Color swatches with alpha checkerboards and HEX/RGBA tooltips. They do not open an editor or change values. |
| Viewport inspection | Hover page elements in edit mode for box layers and an information tooltip without changing selection. |

Switching tabs or languages preserves controls, selection, active USS, searches, foldouts and browsing state. Tab changes clear temporary canvas inspection; language changes update visible inspection text in place. Source matching pauses on the hidden diagnostics page and refreshes when it becomes visible.

## Interpretation and limits

- Preview lines come from UI Builder's generated **in-memory export**, including unsaved edits. They may differ from disk line numbers or formatting.
- UXML navigation uses serialized sibling paths rather than requiring unique names. Internal control children and external template content map to the nearest declaration in the current document; the extension does not automatically open template source files.
- Style matching and cascade order use Unity's native APIs. Related-style menus can list state rules such as `:hover` even when inactive; the source panel shows currently matched declarations.
- Computed values and declared values are distinct. A declared percentage can resolve to a pixel size. The layout table preserves raw Basis/min/max units and uses `hierarchy.parent` for the actual layout parent, including ScrollView content containers.
- Default theme rules, external template rules, UXML inline styles and runtime inline styles can be read-only. A missing navigable declaration is reported rather than replaced with a guessed source.
- Variable declarations navigate to their use, not their definition or fallback chain. Binding notices do not claim to locate C# assignments or fully resolve binding state.
- Canvas outlines follow zoom, pan, scrolling and transforms. The overlay is clipped by the viewport, so ancestor overflow clipping can still leave inspectable bounds outside a child's visible region.
- Cross-line drag selection is limited by per-line virtualization; use the located/full-preview copy actions when needed.
- Native Builder integration uses a small set of internal APIs. Other Unity versions have not been validated. An incompatible interface stops integration with a visible error and restores native controls.

The extension does not edit Unity installation files, PackageCache, UXML/USS, scenes, project settings or runtime scripts. Disabling it restores the same native Inspector controls in their original order. No user document is saved by inspection or language changes.

## Verification and export

`Tests~` contains eight independent PowerShell test runners, including `Run-ExampleAssetsTests.ps1`, plus Editor integration check method bodies for Unity MCP. These test sources are excluded from imported assets and exported packages. See the Chinese README for the complete command list.

Export with PowerShell 7:

```powershell
& 'Packages/com.poorl.ui-builder-navigator/Tools~/Export-Package.ps1'
```

The default output is `Builds/Tools/UIBuilderUssNavigator.unitypackage`. Export reads only plugin files and their metadata; it does not invoke global Unity asset saves.

## License

Licensed under the [MIT License](LICENSE.md). Commercial use, modification and redistribution are permitted with the copyright and license notices preserved.
