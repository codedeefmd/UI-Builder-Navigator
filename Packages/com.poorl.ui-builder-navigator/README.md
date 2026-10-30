# UI Builder USS Preview Navigator

[English](README.en.md) | 中文

给 Unity UI Builder 底部的 **UXML Preview 与 USS Preview** 增加语法配色和选择联动，并在右侧 Inspector 的独立“诊断”页显示计算盒模型与只读样式来源：选择页面元素时定位到 UXML 开始标签，查看属性的最终值、声明来源和覆盖链，点击来源定位 USS 规则。

这是独立的 Editor UPM 包。开发工程通过 `Packages/com.poorl.ui-builder-navigator` 嵌入本包；其他工程可通过 Package Manager 的本地 package.json 或 Git 子目录 URL 安装。包不依赖原业务项目的代码或资源，也不添加第三方依赖。

## 界面语言（zh / en）

Inspector 顶部右侧有 **中文 / English** 语言菜单，切换即时生效。首次使用跟随系统语言（中文系统为 zh，其他系统为 en），之后独立保存在当前用户的 EditorPrefs，关闭/重开插件和 Unity 重启后保留。也可以通过 **Window → UI Toolkit → USS Preview Navigator → 语言 / Language** 切换；扩展关闭后该顶部菜单仍然可用。“启用 / Enabled”菜单名也随语言更新。

语言覆盖插件自有的属性/诊断页签、盒模型说明、父子布局表、样式来源分组和覆盖链、继承/内联/默认/变量/绑定提示、颜色块说明、画布悬浮信息、搜索及复制菜单、相关样式菜单、空状态和接口错误。用户源码、USS 属性名和关键字、选择器、文件名、元素类型/name/class 保留原文；Unity 原生 Inspector、菜单及 UI Builder 自带标题沿用 Unity 自己的语言。

切换只更新现有控件的文字，保留当前选择、活动 USS、UXML/USS 定位、搜索词/结果/当前项、折叠和滚动状态。分组和来源类别使用稳定键/枚举，译文不参与匹配、解析或缓存身份。隐藏的诊断页记录语言失效，返回可见页后再读取来源链。

## 使用

1. 打开 UI Builder 和任意引用了 USS 的 UXML。
2. 显示底部 USS Preview。在左侧 StyleSheets 中单击一个选择器，例如 `.login-panel`。
3. 底部会显示该规则的行范围，并自动滚动、高亮规则。USS 的临时定位蓝底在随后点击或离开 UI Builder 焦点时取消；再次选择样式会重新定位。
4. 在 Hierarchy 或画布中选择元素，左侧 UXML Preview 会跳到对应的开始标签；再次点击同一个元素也会重新定位。标签跨行时高亮整个开始标签，不展开选择其全部子节点。
5. 右键 Hierarchy 中的元素，展开 **相关样式**，选择如 `.card [PlatformStyles.uss]` 的菜单项；插件会切换活动 USS 并定位到规则，同时保留 Hierarchy、画布和 Inspector 中正在处理的元素。
6. 两侧代码预览标题下都有独立搜索框。输入文字即可查找完整预览，黄色显示全部命中，橙色强调当前结果；右侧显示“当前结果/总数”。用 ↑/↓ 或 `Enter`/`Shift+Enter` 循环切换，`Esc` 或框内 × 清空。在对应代码区按 `Ctrl+F` 可聚焦该侧搜索框（macOS 为 `Cmd+F`）。
7. 选中页面元素后，切到 Inspector 顶部第二个页签 **诊断**，展开 **样式来源（只读）**。每条属性显示当前计算值、胜出声明及所在规则；展开“被覆盖的声明”查看较低优先级的实际匹配声明。点击可用的来源按钮，左侧样式列表与底部 USS 预览一起定位，当前页面元素和 UXML 标签定位保持。
8. 在 **盒模型（计算值）** 内展开 **父容器 ↔ 当前元素（布局）**。两列对照父子布局信息，悬停表格时在画布中同时显示紫色父内容区与蓝色当前外框。对照区域默认折叠，展开后随诊断页统一滚动；属性页保留原生 Inspector。

搜索按文字逐行匹配、忽略大小写，同一行的多处匹配分别计数。结果跳转会把目标行和过长行中的目标文字滚动到可见位置。两侧查询互不影响，内存预览更新后会重新计算结果；元素/样式定位与搜索各自保留高亮，最近一次明确导航决定滚动位置。

选择相关样式时，左侧 StyleSheets 列表也会展开所属文件、滚动到对应规则，并显示独立的蓝色定位标记。只有搜索条件挡住目标时才清除筛选；正常搜索结果会保留。标记不使用原生选择状态，Inspector 继续显示页面元素。手动滚动离开目标后不会被拉回，虚拟行复用时也不会残留标记。

UXML 的蓝底表示当前元素的标签位置，会在打开菜单、编辑 Inspector 或切换相关样式时持续保留。查看相关样式不会把原生选择切换成 selector，因此 UXML 与 USS 可以同时高亮；改选其他元素时，UXML 定位随之更新。

右侧 Inspector 的诊断页提供可折叠的 **盒模型（计算值）**，随当前页面元素显示 `margin → border → padding → content` 四层。每层分别标出上、右、下、左的最终数值；中心为内容区宽高，下方另列包含 padding 与 border、排除 margin 的外框尺寸。例如外框 `461 × 200`、四边 padding 为 `30`、border 为 `1` 时，内容区为 `399 × 138`。

盒模型只读，属性继续通过原生 Inspector 编辑。修改布局或窗口尺寸后数值自动更新；查看相关样式时仍显示原页面元素。图形各层是固定比例示意，数字才是计算结果；百分比、auto、flex 等显示为布局后的值，UI Builder 的画布缩放不会改变数值。属性编辑与只读诊断通过顶部页签切换。

**父子布局对照**提供 17 行并列信息：元素标识、外框、内容区、排列方向、换行、主轴对齐、交叉轴对齐、多行对齐、Grow、Shrink、Basis、自身对齐、定位以及四项 min/max 尺寸限制。外框和内容区来自 `layout` / `contentRect`，显示布局后的 px；Basis 和 min/max 读取级联后的原生 `Length`，保留 `%`、`px`、`auto`、`none`，与布局结果分开。原生零长度可能显示为 `0`。表格只更新已有文字，不重建控件或改变用户的滚动和折叠状态。

父容器读取 `element.hierarchy.parent`，即实际参与布局的父级。Unity 6.3 的 `element.parent` 可以是逻辑容器，例如 ScrollView 子元素的逻辑父级是 ScrollView，实际布局父级则是 `contentContainer`；本面板显示并高亮后者。父内容区仍包含兄弟元素占用的空间。没有父级或元素尚未挂入 Panel 时显示对应状态，不向上寻找替代父级。

鼠标移入整张表格后，紫色范围为父 `contentRect`，蓝色范围为当前元素的 border-box（包含 padding/border，排除 margin）。各自逐角执行局部到覆盖层的坐标转换，跟随父子缩放、旋转、平移、画布缩放与 ScrollView 滚动；表格数字保持未缩放的布局单位。信息卡沿用现有低饱和度语义配色。覆盖层忽略拾取，画布点击、当前元素选择、UXML/USS 定位均由原有路径处理。

父子对照、盒模型逐层悬停、Viewport 检查共享单个覆盖层，并通过明确的 owner 定向释放。移出表格、折叠、选择变化、取消选择、元素脱离 Panel、Preview 模式、窗口失焦、关闭插件或脚本重载时清理；旧入口迟到的离开事件不会清掉新入口的高亮。高亮显示完整布局几何，仅由整个 Viewport 裁剪；祖先 overflow 或 ScrollView 实际裁掉的部分也可能继续显示，方便观察范围。表格提供布局事实，不自动判定布局根因或推荐改写样式。

Inspector 顶部提供 **属性 / 诊断** 两页。属性页保留 Unity 原生控件、编辑能力和滚动位置；诊断页集中盒模型、父子布局对照与样式来源，整页共用一条纵向滚动条，不再由两个小滚动区挤压原生属性。两页切换保留折叠与滚动状态，当前元素及 UXML/USS 定位不变；切页会清除临时画布高亮。停留属性页时不查询隐藏的来源链，返回诊断页时重读当前未保存状态。当前页签保留在本次 Editor 会话中，脚本重载后继续使用；关闭插件后原生 Inspector 节点按原顺序恢复。

右侧诊断信息采用独立的语义配色：属性名为浅蓝、枚举/关键字为淡紫、数值为柔和绿、单位为灰蓝；元素类型为浅金色、`#name` 为浅棕色，选择器为青绿色、来源文件为灰蓝色，说明文字降低亮度。普通正文由 11px 增至 12px，元素标识/分组/最终值使用适当的 13px 强调。`column`、`nowrap`、`stretch`、`auto`、Unity 枚举和数值/单位分别显示；`579px` 与 `579 px` 都正确区分单位。字体继续沿用 Unity 编辑器，深浅主题各自有配色。底部 UXML/USS 代码预览沿用原有高亮器。

**样式来源**按文字、外观、尺寸与布局、间距与边框分组，覆盖 33 项常用长属性。计算值来自当前元素的 `resolvedStyle`；下面的声明保留 Unity 内存中的 USS 表达式。例如 `width: 50%` 的最终宽度可能显示 `360 px`，两者分别解释“声明了什么”和“当前布局算出了什么”。未找到显式声明时显示“默认 / 引擎布局”，不会捏造一个可跳转的规则。

声明链沿用 Unity 原生 `MatchedRulesExtractor` 的真实状态匹配和级联顺序：默认主题、specificity、样式表层级、规则顺序都由 Unity 决定。简写按原生接口展开到长属性；同一逗号分组的多个分支匹配时只保留优先级最高的一次，同规则内重复声明和同名规则分别保留。与“相关样式”菜单不同，来源面板不把此刻尚未生效的 `:hover`、`:focus` 等状态规则列为生效来源。

UXML 内联规则和 C# / 运行时 `IStyle` 写入分开显示；继承属性会追到真正声明它的祖先，同时保留当前子元素作为导航上下文。`initial`、`unset` 保留胜出声明，并说明值来自初始值或父级；显式 `unset` 的可继承属性另列父级来源。包含变量的声明定位到变量使用处，本版不追踪变量定义、fallback 或解析失败链；显示的数值仍是 Unity 实际计算结果。检测到受支持属性的数据绑定时会提示，声明清单不冒充绑定的解析状态或 C# 赋值代码位置。

颜色值旁显示 16px 只读色块；半透明或透明颜色使用棋盘底，悬停可看八位 HEX 和浮点 RGBA。最终值色块直接取 `resolvedStyle` 的原生 Color，包含变量解析、继承和运行时变化后的实际结果。明确的 USS/C# 颜色常量（包括 `white`、`transparent`）在声明行旁也显示色块；`var()`、`initial`、`unset` 保留其表达式，不伪造常量色块。色块不使用 ColorField，不弹出颜色编辑器，不修改来源。非颜色值更新后会收起原色块。

默认主题或不在当前文档样式树中的规则、UXML 内联和运行时内联均为只读来源，按钮不可跳转。来源面板只解释 Builder 当前 live 元素，不能据此推断 Game View / Player 的全部运行环境。改变属性继续使用 Unity 原生 Inspector。

面板沿用现有刷新周期。规则匹配 hash、祖先链、受支持的运行时内联 / 绑定或内存预览变化时才重建来源链；空闲时只轻量更新计算值，保留滚动和折叠状态。折叠面板时停止来源查询，展开后重新读取。读取器每轮清理原生匹配器集合，不保留旧样式规则。

鼠标悬停 `margin`、`border`、`padding` 或 `content` 色层时，会强调当前色层，并在画布中的真实元素上叠加对应的盒模型颜色。元素旁的信息条显示类型/name、外框尺寸与当前层的四边数值或内容尺寸。覆盖层跟随画布缩放、平移和元素变换，忽略鼠标拾取；移出盒模型、改选元素、折叠面板或窗口失焦时消失，不改变原生选择、代码定位或文档内容。

在 **Viewport 编辑模式** 中，直接把鼠标移到页面元素上，也会临时显示其四层盒模型，以及类型/name、class、外框与内容区尺寸、字号、margin/border/padding 的四边数值。无需先点击选中元素。悬停只查看信息，Inspector、右侧盒模型和 UXML/USS 定位继续对应原来选中的元素；点击仍由 Unity 原生逻辑正式选中。移出 Viewport、开始按键拖动、窗口失焦或进入 Preview 模式时，临时信息收起。右侧盒模型与 Viewport 复用一个覆盖层，切换入口不会留下重复色层。

悬浮信息采用不透明的深灰底，避免页面文字透入。类型为柔和蓝色、name 为浅棕色、class 为淡紫色；数值用浅绿色，属性名、方向和单位用不同明度的灰蓝色。content、margin、border、padding 标签分别呼应蓝、橙、黄、绿四层盒模型。文字适当增大并增加内边距，便于连续查看密集数据。

“相关样式”使用 Unity 原生本地样式匹配器，包含类型、name、class、组合选择器以及 `:hover`、`:focus` 等相关状态规则；状态规则被列出不代表它此刻已经生效。每项显示所属 USS 文件，同名规则独立保留；当前文档样式树之外的规则显示为只读。右键普通编辑器控件不会出现此子菜单，Unity 原有的复制、重命名和删除菜单继续保留。

UXML 定位沿用 Unity 序列化节点的父子关系，因此不要求元素具有唯一的 name 或 class。模板内部元素和控件自动生成的子节点会定位到当前文档中声明它们的最近 Instance/控件标签；不会自动打开或替换模板源文件。

开关位于 **Window → UI Toolkit → USS Preview Navigator → 启用 / Enabled**（按当前语言显示），默认启用。关闭后恢复 Unity 原生的代码预览并移除盒模型与样式来源面板；开关保存在当前用户的 EditorPrefs 中，其他安装了本插件的项目使用同一偏好。

USS 区分选择器、属性名、数值与单位、字符串、函数和注释；UXML 区分标签名、属性名、属性值、注释和文本。配色跟随 Unity 编辑器的深色/浅色主题，在文本或主题改变时缓存生成，不在每次滚动时重新扫描。

## 行号和编辑状态

行号对应 **UI Builder 内存中生成的 UXML/USS 预览文本**。Unity 导出时会整理格式、转换部分值、移除源文件注释，因此这些行号不一定等于磁盘文件的行号。未保存的结构和样式修改也参与预览。

原生预览会在文本较长时截断。插件读取截断前的完整导出结果，用虚拟化列表按行显示；只创建可见行控件，不把整个文件放进一个巨大的文本元素。选择器位于文件后半部分时也能定位。

高亮范围是完整规则。逗号分组中的选择器共享同一个规则块；同名选择器重复出现时，按左侧实际选中的规则区分。选择普通 UXML 元素时不自动跳到某条规则，也不改变右侧 Inspector 的选择。

自动定位蓝底与手动文字选区相互独立：取消 USS 临时蓝底不会重建代码行，也不会清除正在进行的手动选择。两侧代码行支持原生鼠标选字、双击选词和 `Ctrl+C`。当前按行虚拟化结构的拖选范围限于单行；跨行复制使用右键 **复制定位规则 / 复制定位标签 / 复制完整预览**，USS 提供规则复制，UXML 提供开始标签复制，两侧均提供完整复制。标题栏原有的外部编辑器按钮保留。

颜色只用于显示，复制内容仍是原始代码，不包含配色标签；源代码里的 `<color>`、XML 实体等字样也按原文显示。

## 支持范围

- 开发与验证版本：**Unity 6.3 LTS / 6000.3.27f1，Windows Editor**。
- 仅在 Editor 程序集内编译，不进入 Windows Player。
- 不修改或替换 Unity 安装目录、PackageCache、UXML/USS、业务 C#、场景或 PanelSettings。
- 没有公开的 UI Builder 代码预览扩展接口，因此少量内部状态通过反射读取。相关成员集中在 `Editor/BuilderAccess.cs`；未验证其他 Unity 版本。
- 内部接口不匹配时输出明确的错误，停止本次联动并恢复原生面板；重新开启菜单开关可以重试。不会反复刷异常或把失败当成成功。

关闭 UI Builder、脚本重载或关闭插件开关时，插件移除自己的面板控件并还原原生预览，不保存当前文档。

## 文件职责

| 文件 | 职责 |
| --- | --- |
| `Editor/L10n.cs` | zh/en 语言偏好、格式化和版本通知；不修改源码或项目设置。 |
| `Editor/NavigatorMenu.cs` | 当前语言下的顶部启用/语言菜单，隔离 Unity 动态菜单接口并在重载前清理自有路径。 |
| `Editor/BuilderAccess.cs` | 读取原生选择、完整预览和序列化节点路径，隔离内部 API 的版本依赖。 |
| `Editor/UssPreviewNavigator.cs` | 插件开关、窗口生命周期、两侧选择定位和点击取消提示。 |
| `Editor/RelatedStylesMenu.cs` | 页面元素的“相关样式”子菜单、原生规则匹配、跨 USS 导航。 |
| `Editor/StyleRuleListLocator.cs` | 左侧样式树的展开、滚动与独立定位标记，不改动原生选择。 |
| `Editor/InspectorDiagnosticsTabs.cs` | Inspector 属性/诊断页切换、单一外层滚动区和原生控件的挂载/恢复，不改原生字段或文档。 |
| `Editor/InspectorBoxModel.cs` | Inspector 诊断页的只读盒模型，读取最终四边数值、内容区与外框尺寸。 |
| `Editor/InspectorLayoutComparison.cs` | 盒模型内的父子布局对照表，读取实际布局父级与保留单位的长度，将表格悬停交给盒模型协调。 |
| `Editor/DiagnosticText.cs` | 不依赖 Unity 的诊断语义配色：属性、数值、单位、关键字、标识和声明；复用安全字面量转义。 |
| `Editor/DiagnosticValue.cs` | 诊断值的显示缓存和可选只读 RGBA 色块；原文与富文本分开，棋盘底显示透明度。 |
| `Editor/StyleSourceReader.cs` | 封装原生真实匹配、声明顺序、内联 / 继承追踪和来源缓存失效；最终值读取与声明来源分开。 |
| `Editor/InspectorStyleSources.cs` | Inspector 只读来源视图、覆盖链折叠与来源导航入口，复用现有行保留浏览状态。 |
| `Editor/CanvasBoxModelOverlay.cs` | 三种入口共用的画布调试层、明确的所有权释放、盒模型色层及父子范围逐角转换。 |
| `Editor/ViewportElementHover.cs` | Viewport 的只读悬停入口，沿用原生文档拾取，处理两种悬停入口的交接与退出。 |
| `Editor/SyntaxCodePreview.cs` | 两种语言共用的虚拟化视图、临时定位背景、原生行内选字和复制。 |
| `Editor/PreviewSearchBar.cs` | 每侧独立的搜索框、命中计数、键盘导航与搜索状态。 |
| `Editor/CodePreviewSearch.cs` | 不依赖 Unity 的文字匹配与精确字符标记，保留语法颜色和源码字面量。 |
| `Editor/UssRuleLocator.cs` | 不依赖 Unity 的规则定位；返回从零开始、首尾均包含的行范围。 |
| `Editor/UxmlElementLocator.cs` | 用序列化节点路径匹配 UXML 结构，排除样式、模板声明和属性覆盖元数据，返回开始标签行范围。 |
| `Editor/UssSyntaxHighlighter.cs` | 不依赖 Unity 的 USS 词法配色和原文字面量转义，跨行保留注释与字符串状态。 |
| `Editor/UxmlSyntaxHighlighter.cs` | 不依赖 Unity 的 UXML 词法配色，跨行保留标签、属性字符串和注释状态。 |
| `Editor/SyntaxText.cs` | 两种高亮器共用的源码字面量转义。 |
| `Editor/UIBuilderUssNavigator.Editor.asmdef` | 将插件限制为 Editor 程序集。 |
| `Tests~/` | 独立定位和语法配色测试；Unity 不导入以 `~` 结尾的测试目录。 |

## 独立测试

以下详细 Editor 交互记录来自迁移前工程，作为迁移基线保留；新开发工程尚未添加 Demo 页面。历史截图未迁入本仓库。独立测试可直接在本工程运行。

在 PowerShell 中运行（每个脚本独立进程，避免 Add-Type 重复加载同名类型）：

```powershell
pwsh -NoProfile -File 'Packages/com.poorl.ui-builder-navigator/Tests~/Run-UssRuleLocatorTests.ps1'
pwsh -NoProfile -File 'Packages/com.poorl.ui-builder-navigator/Tests~/Run-UssSyntaxHighlighterTests.ps1'
pwsh -NoProfile -File 'Packages/com.poorl.ui-builder-navigator/Tests~/Run-UxmlSyntaxHighlighterTests.ps1'
pwsh -NoProfile -File 'Packages/com.poorl.ui-builder-navigator/Tests~/Run-UxmlElementLocatorTests.ps1'
pwsh -NoProfile -File 'Packages/com.poorl.ui-builder-navigator/Tests~/Run-CodePreviewSearchTests.ps1'
pwsh -NoProfile -File 'Packages/com.poorl.ui-builder-navigator/Tests~/Run-DiagnosticTextTests.ps1'
pwsh -NoProfile -File 'Packages/com.poorl.ui-builder-navigator/Tests~/Run-LocalizationTests.ps1'
```

测试使用 PowerShell 自带的 `Add-Type` 编译，不安装测试框架。定位器覆盖精确选择器、同名规则、分组、组合器、LF/CRLF、`@import`、注释、含花括号的字符串和不完整规则。配色器覆盖常见 USS 标记、跨行注释与字符串、深浅主题，以及含 `<color>`、`</noparse>` 的源码逐字符往返。`Tests~` 属于源码测试目录，Unity 导出的 `.unitypackage` 不包含这个被忽略的目录。

在 Unity 6000.3.27f1 的实际 UI Builder 中已验证：43 条独立定位断言通过；当前文档引用的 5 份样式表、448 个选择器全部匹配；1628 行完整预览可定位到末尾规则；跨样式表切换、内存预览更新、规则复制、开关恢复以及手动浏览位置保留均通过。测试期间文档和场景保持未修改状态。

语法配色新增 73 条独立断言并通过 Unity 编译；实际窗口已确认不同标记显示不同颜色，规则蓝底保留，复制的 6 行 `.role-switch` 规则与原始文本一致且不含配色标签。亮色主题的调色板由独立测试覆盖，实际窗口截图验证使用深色主题。

补充 UXML 配色后，43 项定位、73 项 USS 配色、54 项 UXML 配色共 170 项断言全部通过。实际 UI Builder 中验证了两侧配色、点击取消定位背景、轮询不重新点亮、重复点击同一条样式重新定位，以及主动行内选字后复制得到原始代码。

UXML 元素定位新增 48 项断言，合计 218 项通过；覆盖重复匿名节点、嵌套、多行标签、引号内 `>`、元数据与自定义命名空间。当前 Introduction 页 54 个元素的序列化路径全部匹配正确；现场确认 Hierarchy 重复点击、画布点击、预览点击取消以及开始标签原文复制均有效。

相关样式菜单现场检查：54 个页面元素共列出 136 条相关规则，全部可定位，涉及 PlatformStyles、PlatformPages、PlatformIcons。执行实际右键菜单动作后，`.card` 定位到 L123–130；跨表选择 `.pages-root [PlatformPages.uss]` 会切换 USS 并定位到 L5–7。原生 Copy、Rename、Delete 等菜单仍存在。

双预览联动检查记录在 `Tests~/LinkedPreviewChecks.cs.txt`，可通过 Unity MCP 的 `execute_code` 在选中页面元素后执行。覆盖右键保留 UXML、切换相关规则不替换 Inspector 的元素选择、两侧同时高亮、取消 USS 提示不影响 UXML、改选元素撤销旧规则，以及页面内存内容不被修改。

盒模型在 Unity 6000.3.27f1 实际窗口中验证：用独立于文档的临时元素检查 50% 宽度、非对称 padding/border、负 margin 和 Unity 像素取整后的四边值；固定外框修改 padding 时，中心内容区会更新。元素移除、面板折叠、插件关闭/重开均正常，原生元素选择与内存 UXML 保留。临时验证元素已移除，218 项独立断言重新通过，Console 无编译错误。

悬停回归方法体位于 `Tests~/HoverOverlayChecks.cs.txt`，可通过 Unity MCP `execute_code` 在选中页面元素后运行。已验证四层真实鼠标拾取、从内层退回外层、画布鼠标穿透、移出隐藏且轮询不重现、原生选择/内存文档/双预览定位保持。实际将画布缩放从 50% 改为 75% 后，色层尺寸随之变为 1.5 倍；平移 `(24, 16)` 后覆盖层同量移动，验证后已恢复原视口。折叠、失焦和关闭插件也会清除临时覆盖层。

搜索核心新增 79 项独立断言，覆盖大小写、CJK/Emoji、重复命中、跨语法 token 和原文含富文本标签时的逐字还原。`Tests~/SearchPreviewChecks.cs.txt` 验证实际输入框的 Enter/Shift+Enter/小键盘 Enter、首尾循环、两侧隔离、无结果禁用、Esc 清空，以及原生选择和文档保持；另在实际预览中验证长行结果的水平滚动与文件末尾结果的虚拟行定位。

`Tests~/ViewportHoverChecks.cs.txt` 验证真实 Viewport MouseMove 路由与原生拾取结果一致、未选中时也可显示、目标切换、移出/拖动/Preview 时隐藏、鼠标穿透，以及原生选择、内存 UXML/USS 和代码定位保持。现场另验证原生点击仍可选中元素，右侧盒模型原有悬停回归通过，两个入口往返交接不会替换当前选中元素；临时测试选择在检查后恢复。

样式来源的实际引擎检查按 `StyleSourcesSetup.cs.txt → 等一轮布局 → StyleSourcesCacheSetup.cs.txt → 等一轮样式计算 → StyleSourcesChecks.cs.txt` 执行。临时样式表通过原生 importer 在内存建立，测试元素在用户文档之外、画布之外，检查结束移除。覆盖百分比与实际尺寸、specificity、同名 / 重复 / 分组规则、简写覆盖、UXML / C# 内联、祖先继承、真实 hover、initial / unset、有效及失败变量，以及 class、伪状态、祖先链、运行时内联、绑定增删的缓存失效。`StyleSourcesNavigationChecks.cs.txt` 通过实际来源按钮确认元素选择与双预览联动，并检查空闲时复用来源数据、保持列表和滚动位置；原生选择和活动 USS 在 finally 恢复。已在 Unity 6000.3.27f1 通过这些检查，内存 UXML/USS 内容保持。

父子对照通过三组实际 Editor 检查：`LayoutComparisonSetup.cs.txt → 等布局 → LayoutComparisonChecks.cs.txt` 覆盖父内容区/外框、布局参数、百分比与关键字、独立旋转缩放的坐标、ScrollView 实际布局父级与滚动、重新挂载和 Panel 脱离；`LayoutComparisonHoverSetup.cs.txt → 等布局 → LayoutComparisonHoverChecks.cs.txt` 覆盖真实 panel.Pick / PointerMove、三个入口往返交接、迟到 Leave、Preview、折叠、取消检查和只读边界；`LayoutComparisonZoomSetup.cs.txt → 等布局 → LayoutComparisonZoomChecks.cs.txt` 临时将实际画布放大 1.25 倍，确认轮廓同比变化而布局尺寸不变。检查结束会移除临时节点，恢复原有 Preview、画布缩放/偏移、面板折叠/滚动状态。Unity 6000.3.27f1 已通过上述检查，原有盒模型/Viewport 悬浮回归通过。

诊断配色新增 89 项独立断言，覆盖深浅主题、未知 Unity 枚举、独立/连续单位、科学计数法、声明、元素标识及富文本注入/原文往返。`DiagnosticInspectorChecks.cs.txt` 在 StyleSources 临时样例和已展开的实际布局表上，验证原生颜色常量、命名颜色、变量最终值、失败继承、RGBA alpha、无编辑控件、色块隐藏及实际语义标签；原文保留在 `DiagnosticValue.TextLabel.userData`。`DiagnosticAppearanceSetup.cs.txt` / `DiagnosticAppearanceRestore.cs.txt` 用于临时选中示例并恢复 UI 状态。来源、缓存、布局数据、整表拾取/交接与来源按钮回归通过，页面内存内容保持。

Inspector 页签在 Unity 6000.3.27f1 中通过 `InspectorTabsChecks.cs.txt` 验证互斥/重复点击单选、隐藏页停止来源查询、诊断无嵌套 ScrollView、三种高亮切页释放、原生控件身份/层级/顺序/内联样式恢复，以及选择和内存文档保持。跨实际布局帧按 `InspectorTabsScrollSetup.cs.txt → 等布局 → InspectorTabsScrollChecks.cs.txt（共三次，每次间隔一轮布局）` 执行，确认往返切页保留两页滚动且当前页填满 Inspector。来源按钮、整表真实拾取及高亮交接回归通过，实际关闭/重开插件能恢复并复用原生控件。

国际化新增 18 项独立断言：默认语言、zh/en 往返、幂等切换、通知/版本、独立偏好、格式化和不支持语言的无状态变更拒绝。总计 404 项纯测试通过。`Tests~/LocalizationChecks.cs.txt` 在同一原子调用内临时选择真实页面按钮，再执行 zh/en/zh：覆盖页签/标题/表头/覆盖链、搜索提示和复制/相关样式菜单、静止父子悬浮信息、同色只读色块 tooltip；同时检查控件身份、搜索词/结果/序号、折叠/滚动、覆盖层、原生选择和内存源码保持。实际语言菜单动作与 EditorPrefs 也通过；隐藏页不查询，重显后按新语言读取。英文环境下来源解析/缓存/变量/继承及颜色样本回归通过。

## 导出插件包

源码目录中使用 PowerShell 7 与 Windows 自带 tar 导出：

```powershell
& 'Packages/com.poorl.ui-builder-navigator/Tools~/Export-Package.ps1'
```

默认输出到 `Builds/Tools/UIBuilderUssNavigator.unitypackage`。脚本只读取插件文件与 `.meta`，不调用 Unity 的全局资源保存；避免导包时连带写入动态字体缓存并触发页面依赖重导入。`Tools~` 与 `Tests~` 一样是源码辅助目录，不进入导出的包。

## 移除

关闭菜单开关，再从项目移除整个 `Packages/com.poorl.ui-builder-navigator` 文件夹即可。没有需要撤销的 Unity 程序集或业务资源补丁。

## 相关官方说明

- [Unity 6.3 contentRect：排除 padding 和 border 的内容区](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/UIElements.VisualElement-contentRect.html)
- [Unity 6.3 resolvedStyle：最终布局样式值](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/UIElements.VisualElement-resolvedStyle.html)
- [Unity 6.3 UI Builder 界面说明](https://docs.unity3d.com/6000.3/Documentation/Manual/UIB-interface-overview.html)
- [Unity 6.3 原生 USS Preview 的选择刷新实现](https://github.com/Unity-Technologies/UnityCsReference/blob/6000.3/Modules/UIBuilder/Editor/Builder/Previews/BuilderUssPreview.cs)
- [Unity 6.3 原生代码预览的文本截断实现](https://github.com/Unity-Technologies/UnityCsReference/blob/6000.3/Modules/UIBuilder/Editor/Builder/Previews/BuilderCodePreview.cs)
- [Unity 6000.3.27 原生匹配提取器](https://github.com/Unity-Technologies/UnityCsReference/blob/32dee660a55df47c2c02fce871533e39549a6e7e/Modules/UIElements/Core/StyleSheets/MatchedRulesExtractor.cs)
- [Unity 6000.3.27 原生级联排序](https://github.com/Unity-Technologies/UnityCsReference/blob/32dee660a55df47c2c02fce871533e39549a6e7e/Modules/UIElements/Core/StyleSheets/StyleSelectorHelper.cs)
- [Unity 6000.3.27 变量解析失败时的 unset 行为](https://github.com/Unity-Technologies/UnityCsReference/blob/32dee660a55df47c2c02fce871533e39549a6e7e/Modules/UIElements/Core/StyleSheets/StylePropertyReader.cs)
- [Unity 6000.3.27 原生 Length 与计算布局属性](https://github.com/Unity-Technologies/UnityCsReference/blob/32dee660a55df47c2c02fce871533e39549a6e7e/Modules/UIElements/Core/Style/Generated/ComputedStyle.cs)
