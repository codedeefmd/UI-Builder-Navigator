# Reddit draft

Intended community: `r/Unity3D`

Submission status, 2026-11-02: [submitted post](https://www.reddit.com/r/Unity3D/comments/1xllk46/i_built_an_mitlicensed_ui_builder_extension_for/) with the real English Editor video and `Resources/Tutorial` flair. AutoModerator removed it because the account did not meet the reputation requirement: at least 50 total karma or 20 karma in `r/Unity3D`. Keep the existing post; do not delete and repost. After explicit author approval, one moderator-review request was sent on the same day and verified in the Reddit conversation. Its recipient, subject, and body are recorded in `reddit-moderator-review.en.md`. A response is pending.

## Suggested title

I built an MIT-licensed UI Builder extension for linked UXML/USS navigation and layout inspection

## Post body

I’m the author of **UI Builder Navigator**, an open-source, Editor-only extension. I started it while trying UI Toolkit in Unity 6: as a newcomer, I felt UI Builder showed too little information to understand the selected element, and I found the workflow awkward. I kept going back and forth to find its UXML tag, the USS rules affecting it, why a style won, and how the parent affected its layout.

I began building this extension with GPT's help to fill those gaps as I encountered them. The idea was to make the element, its source, its styles, and its computed layout easier to follow inside UI Builder.

The `0.2.0` version currently provides:

- linked UXML/USS source previews with syntax highlighting and independent search;
- element-to-UXML and rule-to-USS navigation, plus a **Related Styles** menu that keeps the selected element active;
- a read-only style-source view for computed values, winning/inherited/inline declarations, overridden rules, and alpha-aware color swatches;
- a computed box model and parent/current layout comparison with canvas bounds overlays; and
- a bilingual three-page Learning Lab with illustrated examples.

It is MIT licensed and has no third-party package dependencies.

Git UPM URL:

```text
https://github.com/codedeefmd/UI-Builder-Navigator.git?path=/Packages/com.poorl.ui-builder-navigator
```

Repository, screenshots, documentation, and issue tracker:
<https://github.com/codedeefmd/UI-Builder-Navigator>

Release and downloads:
<https://github.com/codedeefmd/UI-Builder-Navigator/releases/tag/v0.2.0>

42-second Editor demo with English captions:
<https://github.com/codedeefmd/UI-Builder-Navigator/releases/download/v0.2.0/ui-builder-navigator-demo-en.mp4>

Tested with **Unity 6000.3.27f1 on Windows Editor**. It integrates with internal UI Builder Editor APIs; compatibility with other Unity versions and operating systems is not yet verified.

I’d like feedback from people who actively use UI Toolkit: which missing link in UI Builder slows you down most—source navigation, cascade visibility, or parent/layout inspection? If you test it, please mention your Unity version and OS, especially if you find a template or nested-control case that maps to the wrong source.

## Before posting — not part of the post body

- Recheck the current `r/Unity3D` rules on the day of posting: <https://www.reddit.com/r/Unity3D/about/rules>
- Confirm that another post about this project would not exceed the community’s current frequency limit.
- Attach a real short clip or GIF captured from `0.2.0`; do not use a mock-up as evidence of Editor behavior.
- Reply to technical questions as the author and clearly separate verified behavior from planned work.
