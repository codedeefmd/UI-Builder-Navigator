# Unity Discussions draft

Target category: <https://discussions.unity.com/c/ui/ui-toolkit/201>

## Suggested title

UI Builder Navigator — linked UXML/USS navigation and layout diagnostics for Unity 6

## Post body

Hi! I’m the author of **UI Builder Navigator**, an MIT-licensed, Editor-only extension for Unity UI Builder.

I started this project while trying UI Toolkit in Unity 6. As a newcomer, I felt UI Builder exposed too little information to understand what I was editing. Finding a selected element's UXML tag, the USS rules affecting it, why a style won, or how its parent influenced layout meant a lot of back-and-forth, and I found the workflow awkward.

So I began building an extension with GPT's help to fill those information gaps. **UI Builder Navigator** grew out of the questions I ran into while learning: I wanted to see the selected element, its source, its styles, and its computed layout together inside UI Builder.

The current `0.2.0` release includes:

- **Linked UXML and USS previews:** selecting a canvas or Hierarchy element reveals its UXML opening tag; selecting a stylesheet rule reveals the corresponding USS block.
- **Search and related-style navigation:** both full in-memory previews have syntax highlighting and independent search. A Hierarchy element’s **Related Styles** menu can navigate to a matching USS rule without replacing the current element context.
- **Read-only style-source tracing:** the Diagnostics tab shows computed values, winning declarations, inherited or inline sources, overridden declarations, and alpha-aware color swatches. Navigable sources link back to USS.
- **Layout diagnostics:** a computed box model and parent/current layout comparison show the actual layout parent, Flex values, units, and canvas overlays for the parent content box and current element border box.
- **Bilingual Learning Lab:** three illustrated Chinese/English UXML/USS examples cover element navigation, cascade/style sources, and parent-child layout.

Install it through **Package Manager → Add package from git URL**:

```text
https://github.com/codedeefmd/UI-Builder-Navigator.git?path=/Packages/com.poorl.ui-builder-navigator
```

Then open:

```text
Window → UI Toolkit → USS Preview Navigator
```

The Learning Lab is available under **Open Examples** in the same menu.

Repository and documentation:
<https://github.com/codedeefmd/UI-Builder-Navigator>

Release and downloads:
<https://github.com/codedeefmd/UI-Builder-Navigator/releases/tag/v0.2.0>

42-second Editor demo with English captions:
<https://github.com/codedeefmd/UI-Builder-Navigator/releases/download/v0.2.0/ui-builder-navigator-demo-en.mp4>

The project is licensed under MIT. It has been tested with **Unity 6000.3.27f1 on Windows Editor**. The integration uses internal UI Builder Editor APIs; compatibility with other Unity versions and operating systems is not yet verified.

I would especially appreciate feedback on:

1. whether it opens and integrates correctly on other Unity 6 patch versions;
2. UXML navigation edge cases involving templates, generated controls, or nested containers; and
3. which style-source or parent-layout information is most useful in your daily UI Toolkit work.

If you try it, please include your Unity version and OS with any issue report. Thanks!
