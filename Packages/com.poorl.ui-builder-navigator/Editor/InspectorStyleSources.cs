using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace UIBuilderUssNavigator
{
    /// <summary>
    /// Inspector 中的只读样式来源面板。计算与反射归 StyleSourceReader，导航归 RelatedStylesMenu；
    /// 本类仅维护显示和折叠状态。轮询时更新已有行，不重建整个列表或抢夺用户的滚动、焦点与选择。
    /// </summary>
    internal sealed class InspectorStyleSources : IDisposable
    {
        readonly VisualElement inspector;
        readonly Foldout root;
        readonly Label target;
        readonly Label hint;
        readonly VisualElement properties;
        readonly StyleSourceReader reader;
        readonly RelatedStylesMenu navigation;
        readonly Dictionary<string, PropertyView> views = new Dictionary<string, PropertyView>();
        readonly List<Label> groups = new List<Label>();
        List<StyleSourceReader.Row> rows;
        VisualElement inspectedElement;
        bool darkSkin = EditorGUIUtility.isProSkin;
        bool languageDirty;

        internal bool IsAttached => root.parent == inspector && root.panel != null;

        internal InspectorStyleSources(VisualElement inspector, System.Reflection.Assembly builderAssembly,
            RelatedStylesMenu navigation)
        {
            this.inspector = inspector;
            this.navigation = navigation;
            reader = new StyleSourceReader(builderAssembly);
            root = new Foldout { name = "uss-navigator-style-sources", value = true };
            root.style.flexShrink = 0;
            root.style.marginLeft = root.style.marginRight = 6;
            root.style.marginBottom = 6;
            root.style.borderTopWidth = 1;
            root.style.borderTopColor = new Color(0.5f, 0.5f, 0.5f, 0.35f);
            target = Text(string.Empty);
            target.name = "style-sources-target";
            target.style.fontSize = 13;
            root.Add(target);
            hint = Text(string.Empty, DiagnosticTone.Comment);
            root.Add(hint);
            // 声明列表自然展开，整页滚动由 InspectorDiagnosticsTabs 独占，避免滚轮落入小窗口。
            properties = new VisualElement { name = "style-sources-properties" };
            properties.style.flexShrink = 0;
            root.Add(properties);
            inspector.Add(root);
            RefreshLanguage();
            Refresh(null);
        }

        /// <summary>
        /// 静态标题立即切换；标记来源行在下一次可见刷新中重绘，不在隐藏的属性页发起查询。
        /// 控件本身不重建，因此覆盖链折叠状态、文字选择和诊断页的外层滚动位置都保持。
        /// </summary>
        internal void RefreshLanguage()
        {
            root.text = L10n.Tr("样式来源（只读）", "Style Sources (Read-only)");
            hint.userData = L10n.Tr("数值为布局后的计算结果；来源行保留原始声明。点击可导航的来源查看 USS。",
                "Values are computed after layout; source rows preserve original declarations. Click a navigable source to open its USS rule.");
            hint.text = DiagnosticText.Colorize((string)hint.userData, DiagnosticTone.Comment, EditorGUIUtility.isProSkin);
            foreach (Label group in groups)
            {
                string key = (string)group.userData;
                group.text = DiagnosticText.Colorize(StyleSourceReader.GroupName(key), DiagnosticTone.Text,
                    EditorGUIUtility.isProSkin);
            }
            languageDirty = true;
            reader.Invalidate();
        }

        /// <summary>折叠时停止匹配查询；展开后由会话现有刷新周期读取，不另建轮询器。</summary>
        internal void Refresh(VisualElement element, bool sourceChanged = false)
        {
            bool paletteChanged = darkSkin != EditorGUIUtility.isProSkin;
            darkSkin = EditorGUIUtility.isProSkin;
            if (paletteChanged)
            {
                hint.text = DiagnosticText.Colorize((string)hint.userData, DiagnosticTone.Comment, darkSkin);
                foreach (Label group in groups)
                    group.text = DiagnosticText.Colorize(StyleSourceReader.GroupName((string)group.userData),
                        DiagnosticTone.Text, darkSkin);
            }
            inspectedElement = element;
            bool available = element != null && element.panel != null;
            hint.style.display = properties.style.display = available ? DisplayStyle.Flex : DisplayStyle.None;
            target.text = available ? DiagnosticText.Identity(StyleSourceReader.Describe(element), EditorGUIUtility.isProSkin) :
                DiagnosticText.Colorize(L10n.Tr("选择 Hierarchy 或画布中的元素",
                    "Select an element in the Hierarchy or canvas"), DiagnosticTone.Comment, EditorGUIUtility.isProSkin);
            if (!available || !root.value)
            {
                reader.Invalidate();
                return;
            }
            bool rebuild = reader.NeedsRebuild(element, sourceChanged || paletteChanged || languageDirty);
            if (!rebuild)
            {
                reader.RefreshValues(rows, element);
                foreach (StyleSourceReader.Row row in rows) views[row.Property.Name].SetValue(row.Value, row.ColorValue);
                return;
            }
            rows = reader.Read(element);
            languageDirty = false;
            HashSet<object> navigable = navigation.GetNavigableSelectors();
            string previousGroup = null;
            foreach (StyleSourceReader.Row row in rows)
            {
                if (!views.TryGetValue(row.Property.Name, out PropertyView view))
                {
                    if (previousGroup != row.Property.GroupKey)
                    {
                        Label group = Text(row.Property.Group);
                        group.userData = row.Property.GroupKey;
                        group.style.unityFontStyleAndWeight = FontStyle.Bold;
                        group.style.fontSize = 13;
                        group.style.marginTop = 7;
                        properties.Add(group);
                        groups.Add(group);
                    }
                    view = new PropertyView(properties, row.Property.Name, declaration =>
                        navigation.NavigateRule(inspectedElement, declaration.Sheet, declaration.Selector));
                    views.Add(row.Property.Name, view);
                }
                previousGroup = row.Property.GroupKey;
                view.Set(row, navigable);
            }
        }

        public void Dispose() => root.RemoveFromHierarchy();

        static Label Text(string text, DiagnosticTone tone = DiagnosticTone.Text)
        {
            var label = new Label(DiagnosticText.Colorize(text, tone, EditorGUIUtility.isProSkin))
                { enableRichText = true, parseEscapeSequences = false, userData = text };
            label.style.fontSize = 12;
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.marginTop = label.style.marginBottom = 2;
            return label;
        }

        /// <summary>一条属性显示计算值、胜出声明、继承位置和可展开的覆盖链，按钮只发起导航。</summary>
        sealed class PropertyView
        {
            readonly DiagnosticValue value;
            readonly Label property;
            readonly Button source;
            readonly Button inheritedSource;
            readonly DiagnosticValue declaration;
            readonly Label note;
            readonly Foldout overridden;
            readonly Action<StyleSourceReader.Declaration> navigate;
            readonly List<DeclarationView> otherViews = new List<DeclarationView>();
            StyleSourceReader.Row row;

            internal PropertyView(VisualElement parent, string name, Action<StyleSourceReader.Declaration> navigate)
            {
                this.navigate = navigate;
                var container = new VisualElement { name = "style-source-" + name };
                container.style.flexShrink = 0;
                container.style.marginTop = 4;
                container.style.paddingBottom = 4;
                container.style.borderBottomWidth = 1;
                container.style.borderBottomColor = new Color(0.5f, 0.5f, 0.5f, 0.18f);
                var header = new VisualElement();
                header.style.flexDirection = FlexDirection.Row;
                property = Text(name, DiagnosticTone.Property);
                property.name = "style-source-property";
                property.style.flexGrow = 1;
                header.Add(property);
                value = new DiagnosticValue("style-source-value", DiagnosticText.Value);
                value.style.maxWidth = Length.Percent(58);
                value.TextLabel.style.unityTextAlign = TextAnchor.MiddleRight;
                value.TextLabel.style.fontSize = 13;
                header.Add(value);
                container.Add(header);
                source = SourceButton(() => navigate(row.Winner));
                source.name = "style-source-winner";
                container.Add(source);
                declaration = new DiagnosticValue("style-source-declaration", DiagnosticText.Declaration);
                declaration.style.marginTop = declaration.style.marginBottom = 2;
                container.Add(declaration);
                inheritedSource = SourceButton(() => navigate(row.InheritedSource));
                container.Add(inheritedSource);
                note = Text(string.Empty);
                container.Add(note);
                overridden = new Foldout { value = false };
                overridden.style.fontSize = 12;
                container.Add(overridden);
                parent.Add(container);
            }

            internal void Set(StyleSourceReader.Row next, HashSet<object> navigable)
            {
                row = next;
                property.text = DiagnosticText.Colorize(row.Property.Name, DiagnosticTone.Property, EditorGUIUtility.isProSkin);
                SetValue(row.Value, row.ColorValue);
                SetSource(source, row.Winner, row.KindKey == StyleSourceReader.SourceKind.Inherited ?
                    L10n.Tr("继承 → ", "Inherited → ") : string.Empty, navigable, row.Kind);
                declaration.SetValue(row.Winner?.Text ?? string.Empty, row.Winner?.ColorValue);
                declaration.style.display = row.Winner != null ? DisplayStyle.Flex : DisplayStyle.None;
                SetSource(inheritedSource, row.InheritedSource, L10n.Tr("继承来源 → ", "Inherited source → "),
                    navigable, string.Empty);
                inheritedSource.style.display = row.InheritedSource != null ? DisplayStyle.Flex : DisplayStyle.None;
                note.text = DiagnosticText.Colorize(row.Note, DiagnosticTone.Comment, EditorGUIUtility.isProSkin);
                note.style.display = row.Note.Length > 0 ? DisplayStyle.Flex : DisplayStyle.None;
                int count = Math.Max(0, row.Declarations.Count - 1);
                overridden.style.display = count > 0 ? DisplayStyle.Flex : DisplayStyle.None;
                overridden.text = (row.HasBinding ? L10n.Tr("下层样式声明", "Lower-priority declarations") :
                    L10n.Tr("被覆盖的声明", "Overridden declarations")) + L10n.Format("（{0}）", " ({0})", count);
                // 数值变化不重建覆盖链，保留用户的折叠状态和文字选择；源对象每次更新到 live 引用。
                for (int index = 0; index < count; index++)
                {
                    if (index == otherViews.Count) otherViews.Add(new DeclarationView(overridden, navigate));
                    otherViews[index].Set(row.Declarations[row.Declarations.Count - 2 - index], navigable);
                }
                for (int index = count; index < otherViews.Count; index++) otherViews[index].Hide();
            }

            internal void SetValue(string current, Color? color) => value.SetValue(current, color);

            static void SetSource(Button button, StyleSourceReader.Declaration declaration, string prefix,
                HashSet<object> navigable, string empty)
            {
                bool canNavigate = declaration != null && declaration.Selector != null && navigable.Contains(declaration.Selector);
                bool dark = EditorGUIUtility.isProSkin;
                button.text = declaration == null ? DiagnosticText.Colorize(empty, DiagnosticTone.Keyword, dark) :
                    DiagnosticText.Colorize(prefix, DiagnosticTone.Comment, dark) +
                    (declaration.IsInline ? DiagnosticText.Colorize(declaration.Source, DiagnosticTone.Keyword, dark) :
                        DiagnosticText.Colorize(declaration.SelectorText, DiagnosticTone.Selector, dark) +
                        DiagnosticText.Colorize("  [" + declaration.File + "]", DiagnosticTone.File, dark));
                button.tooltip = declaration == null ? L10n.Tr("没有显式样式声明可跳转",
                    "No explicit style declaration is available for navigation") : declaration.Text +
                    (canNavigate ? L10n.Tr("\n点击定位 USS 规则", "\nClick to locate the USS rule") :
                        L10n.Tr("\n只读来源：当前文档样式列表中没有可导航的规则",
                            "\nRead-only source: no navigable rule exists in the current document style list"));
                button.SetEnabled(canNavigate);
            }

            static Button SourceButton(Action action)
            {
                var button = new Button(action) { enableRichText = true, parseEscapeSequences = false };
                button.style.fontSize = 12;
                button.style.whiteSpace = WhiteSpace.Normal;
                button.style.unityTextAlign = TextAnchor.MiddleLeft;
                button.style.height = StyleKeyword.Auto;
                button.style.minHeight = 23;
                button.style.flexShrink = 0;
                // 只读来源仍保留 disabled 交互状态；文字不随默认 disabled 样式淡到难以阅读。
                button.style.opacity = 1;
                return button;
            }

            sealed class DeclarationView
            {
                readonly VisualElement root;
                readonly DiagnosticValue text;
                readonly Button source;
                StyleSourceReader.Declaration declaration;

                internal DeclarationView(VisualElement parent, Action<StyleSourceReader.Declaration> navigate)
                {
                    root = new VisualElement();
                    root.style.flexShrink = 0;
                    text = new DiagnosticValue("style-source-overridden-declaration", DiagnosticText.Declaration);
                    root.Add(text);
                    source = SourceButton(() => navigate(declaration));
                    root.Add(source);
                    parent.Add(root);
                }

                internal void Set(StyleSourceReader.Declaration current, HashSet<object> navigable)
                {
                    declaration = current;
                    root.style.display = DisplayStyle.Flex;
                    text.SetValue(current.Text, current.ColorValue);
                    SetSource(source, current, string.Empty, navigable, string.Empty);
                }

                internal void Hide() => root.style.display = DisplayStyle.None;
            }
        }
    }
}
