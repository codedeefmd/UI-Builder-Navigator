# UI Builder Navigator Learning Lab

Learning Lab 是一组不依赖业务代码、外部字体或外部图片的 UI Builder 学习页面。三页示例分别覆盖元素与源码导航、USS 级联阅读、父子布局对照。中文与英文页面使用相同的结构、`name` 和 `class`，仅文案不同。

## 导入与打开

推荐从插件菜单直接打开：

- **Window > UI Toolkit > USS Preview Navigator > 打开示例 > 01 · 元素与代码导航**
- **Window > UI Toolkit > USS Preview Navigator > 打开示例 > 02 · 样式来源与覆盖**
- **Window > UI Toolkit > USS Preview Navigator > 打开示例 > 03 · 父子布局与盒模型**

菜单会按插件当前语言打开对应的 `.zh.uxml` 或 `.en.uxml`。示例首次通过菜单打开时，会把该文档画布初始化为 1280×900，并在代码预览折叠时将其展开；已有的画布设置和已导入练习会保留，不会被覆盖。

也可以沿用 Unity 原生方式：

1. 在 Unity 的 **Package Manager** 中选择 **UI Builder Navigator**。
2. 打开 **Samples**，导入 **Learning Lab**。
3. 在 `Assets/Samples/UI Builder Navigator/<package version>/Learning Lab` 中选择需要的 `.uxml`。
4. 双击 UXML，在 UI Builder 中打开；建议先使用宽画布查看整体布局，再缩到约 350px 检查单列换行。

Package Manager 直接导入和双击资产不会触发上述初始化，而是沿用 UI Builder 的原生或该文档已有设置。每页都提供 `.zh.uxml` 和 `.en.uxml`，可直接打开任一语言版本，不需要进入 Play Mode。

## 01 · Navigation：元素与源码导航

打开：

- `01-Navigation/Navigation.zh.uxml`
- `01-Navigation/Navigation.en.uxml`

目标：从画布或 Hierarchy 中的元素，定位它的 UXML 标签和真实相关 USS 规则。

操作：

1. 选择 `#select-me-button`。
2. 在底部 UXML Preview 中确认定位到对应的 `<Button>` 标签。
3. 右键该元素，打开“相关样式 / Related Styles”。
4. 选择 `.demo-button.primary`，在 USS Preview 中查看 `Shared/Examples.uss` 的规则位置。
5. 展开 `#demo-scroll`，观察 Unity 为 `ScrollView` 生成的 viewport、content container 和 scroller。
6. 选择 `#template-card-instance`，再打开 `Shared/ProfileCard.*.uxml`，比较模板实例与模板内部节点的源码归属。

预期：普通元素可定位到当前页面标签；相关样式来自实际加载的 USS；模板内部节点属于独立 ProfileCard UXML，不会被误认为写在页面本体中。

## 02 · Cascade：看懂 USS 级联

打开：

- `02-Cascade/Cascade.zh.uxml`
- `02-Cascade/Cascade.en.uxml`

目标：用真实声明比较公共基线、组合选择器、上下文覆盖、父级颜色继承、UXML 内联样式、自定义颜色属性和透明色。

操作：

1. 依次选择 `#cascade-base-button` 与 `#cascade-primary-button`。
2. 查看相关样式，确认规则同时来自 `Shared/Examples.uss` 与 `02-Cascade/Cascade.uss`。
3. 在原生 Preview 中把鼠标移到 `#cascade-hover-button`，观察 `:hover` 外观。
4. 选择 `#cascade-inherited-label`，对照父容器的 `color` 与未声明 `color` 的子元素。
5. 选择 `#cascade-inline-button`，在 UXML Preview 中查看 `style` 属性。
6. 选择 `#cascade-alpha-swatch`，在 USS Preview 中查看 `rgba(..., 0.35)` 与 `var(...)`。

预期：页面展示 Unity 实际计算出的外观和真实匹配声明。示例不声称自动解释某条规则为何被设计为覆盖另一条规则。

## 03 · Layout：父容器与当前元素布局对照

打开：

- `03-Layout/Layout.zh.uxml`
- `03-Layout/Layout.en.uxml`

目标：对照父子画布范围及计算布局值，理解方向、Stretch/Center、百分比宽度、Grow/Shrink、min/max 和 `ScrollView` 的实际布局父级。

操作：

1. 比较 Row 与 Column 示例的父级 `flex-direction`。
2. 依次选择 `#layout-stretch-button` 与 `#layout-center-button`，观察父级对齐方式如何改变子元素范围。
3. 选择 `#layout-percent-child`，核对它与父级宽度的 50% 关系。
4. 选择 `#layout-flex-child`，读取 Grow、Shrink、Basis 与 min/max。
5. 展开 `#layout-scroll`，选择内容项并观察 Unity 自动生成的 content container。

预期：布局页呈现计算事实和父子范围；它不会自动判断布局结果是否符合设计意图，也不会替代开发者的布局决策。

## UI Builder 编辑模式与原生 Preview

- **编辑模式**适合在画布或 Hierarchy 中选择元素、查看 Inspector，并使用 Navigator 的 UXML/USS 定位、相关样式和布局对照功能。
- **Preview** 由 UI Builder 原生提供，适合观察按钮 `:hover` 等伪状态和控件的预览交互。Preview 中的交互不是 Play Mode，也不表示页面具有业务逻辑。
- 这些示例没有附加 C# 行为。按钮不会启动业务流程；字段和 Toggle 只用于展示原生控件结构与样式。
- 调整 UI Builder 的画布尺寸可检查响应式换行；它不等同于构建 Player 后的最终窗口测试。

## 文件结构

```text
LearningLab/
├─ 01-Navigation/
├─ 02-Cascade/
├─ 03-Layout/
└─ Shared/
   ├─ Examples.uss
   └─ ProfileCard.*.uxml
```

`Shared/Examples.uss` 是三页共用的视觉基线；第二页和第三页各自加载一个页面专属 USS，以便在相关样式和 USS Preview 中看到真实的多样式表来源。
