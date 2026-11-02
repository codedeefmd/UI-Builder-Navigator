# UI Builder Navigator v0.2.0 — Initial public release

## 中文

UI Builder Navigator `0.2.0` 是首个公开版本。这是一个仅在 Editor 中运行的 Unity UI Builder 扩展，用于把元素、UXML 标签、USS 规则、计算样式和布局诊断保持在同一工作流中。

### 主要功能

- UXML/USS 完整内存预览：语法配色、行号、独立搜索和结果定位。
- 选择画布或 Hierarchy 元素后定位 UXML 开始标签；选择样式或“相关样式”后定位 USS 规则，并保留当前元素上下文。
- Inspector 独立“诊断”页：只读样式来源、胜出声明、继承/内联来源、被覆盖声明及透明色颜色块。
- 计算盒模型与父容器/当前元素布局对照；悬停时在画布显示父内容区和当前元素外框。
- 中文/英文插件界面，以及三页带图文说明的 Learning Lab 示例。

### 安装

**Git UPM**

在 Package Manager 中选择 **Add package from git URL**，输入：

```text
https://github.com/codedeefmd/UI-Builder-Navigator.git?path=/Packages/com.poorl.ui-builder-navigator
```

**unitypackage**

从本 Release 的 Assets 下载 `UIBuilderUssNavigator.unitypackage`，在 Unity 中选择 **Assets → Import Package → Custom Package…** 导入。

安装后从以下菜单打开：

```text
Window → UI Toolkit → USS Preview Navigator
```

### Learning Lab

在插件菜单中选择 **打开示例**，按顺序体验：

1. `01 · 元素与代码导航`
2. `02 · 样式来源与覆盖`
3. `03 · 父子布局与盒模型`

通过 UPM 安装时，首次打开菜单示例会通过 Unity 原生 Sample API 导入 `Assets/Samples/UI Builder Navigator/0.2.0/Learning Lab`。通过 unitypackage 安装的示例位于 `Assets/UIBuilderUssNavigator/Samples/LearningLab`。示例用于 Editor 内学习。

### 兼容性与边界

- 已验证：**Unity 6000.3.27f1，Windows Editor**。
- 包名：`com.poorl.ui-builder-navigator`；许可证：MIT。
- 插件程序集仅用于 Editor，不进入 Player 构建。
- 插件使用 Unity UI Builder 的内部 Editor API。其他 Unity 版本和操作系统尚未验证；内部接口变化可能导致集成停止并显示错误。
- 插件提供源码导航与只读诊断；查看信息和切换语言不会保存用户 UXML/USS。

仓库与问题反馈：<https://github.com/codedeefmd/UI-Builder-Navigator>

---

## English

UI Builder Navigator `0.2.0` is the first public release of an Editor-only Unity UI Builder extension that keeps elements, UXML tags, USS rules, computed styles, and layout diagnostics connected in one workflow.

### Highlights

- Complete in-memory UXML/USS previews with syntax colors, line numbers, independent search, and result navigation.
- Select a canvas or Hierarchy element to locate its UXML opening tag; select a stylesheet or **Related Styles** entry to locate the USS rule while preserving element context.
- A separate Inspector **Diagnostics** tab for read-only style sources, winning declarations, inherited/inline sources, overridden declarations, and alpha-aware color swatches.
- A computed box model and parent/current layout comparison, with hover overlays for the parent content box and current element border box.
- Chinese/English extension UI and three illustrated bilingual Learning Lab examples.

### Installation

**Git UPM**

In Package Manager, choose **Add package from git URL** and enter:

```text
https://github.com/codedeefmd/UI-Builder-Navigator.git?path=/Packages/com.poorl.ui-builder-navigator
```

**unitypackage**

Download `UIBuilderUssNavigator.unitypackage` from this Release’s Assets, then choose **Assets → Import Package → Custom Package…** in Unity.

Open the extension from:

```text
Window → UI Toolkit → USS Preview Navigator
```

### Learning Lab

Choose **Open Examples** from the extension menu and follow:

1. `01 · Elements & Code Navigation`
2. `02 · Style Sources & Cascade`
3. `03 · Parent Layout & Box Model`

For UPM installs, the example menu imports Unity's native Sample to `Assets/Samples/UI Builder Navigator/0.2.0/Learning Lab` on first use. The unitypackage includes its examples under `Assets/UIBuilderUssNavigator/Samples/LearningLab`. These are Editor learning materials.

### Compatibility and boundaries

- Verified with **Unity 6000.3.27f1 on Windows Editor**.
- Package: `com.poorl.ui-builder-navigator`; license: MIT.
- The assembly is Editor-only and does not enter Player builds.
- The extension integrates with internal Unity UI Builder Editor APIs. Other Unity versions and operating systems have not been validated; an internal API change can stop the integration and produce a visible error.
- The extension provides source navigation and read-only diagnostics. Inspection and language changes do not save user UXML/USS documents.

Repository and issue tracker: <https://github.com/codedeefmd/UI-Builder-Navigator>
