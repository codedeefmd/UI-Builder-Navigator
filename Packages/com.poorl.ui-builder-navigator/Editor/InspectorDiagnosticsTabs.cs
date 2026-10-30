using System;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace UIBuilderUssNavigator
{
    /// <summary>
    /// 在原生 Inspector 内切换“属性 / 诊断”。原生控件整体放入属性页，不复制、不重建字段，
    /// 让 Unity 继续管理元素选择、属性编辑和原来的滚动状态；诊断页独占一个外层滚动区。
    /// 页签选择只存于本次 Editor 会话，脚本重载后保留；不写入页面资源或项目配置。
    /// </summary>
    internal sealed class InspectorDiagnosticsTabs : IDisposable
    {
        const string SessionKey = "UIBuilderUssNavigator.InspectorDiagnostics";
        readonly VisualElement inspector;
        readonly Toolbar toolbar;
        readonly ToolbarToggle propertiesTab;
        readonly ToolbarToggle diagnosticsTab;
        readonly ToolbarMenu languageMenu;
        readonly VisualElement nativePage;
        readonly ScrollView diagnosticPage;

        internal event Action PageChanged;
        internal bool DiagnosticsVisible { get; private set; }
        internal VisualElement Content => diagnosticPage.contentContainer;
        internal bool IsAttached => toolbar.parent == inspector && nativePage.parent == inspector &&
            diagnosticPage.parent == inspector && inspector.panel != null;

        internal InspectorDiagnosticsTabs(VisualElement inspector)
        {
            this.inspector = inspector;
            // 只包裹已有的原生根节点，保留 header、TwoPaneSplitView 及其内部层级和样式。
            // 不逐项改 display：Unity 更新选择时也会更新 header 的 display，不能与页签争写它。
            nativePage = new VisualElement { name = "uss-navigator-properties-page" };
            nativePage.style.flexGrow = 1;
            nativePage.style.minHeight = 0;
            foreach (VisualElement child in inspector.hierarchy.Children().ToArray()) nativePage.Add(child);

            diagnosticPage = new ScrollView(ScrollViewMode.Vertical) { name = "uss-navigator-diagnostics-page" };
            diagnosticPage.style.flexGrow = 1;
            diagnosticPage.style.minHeight = 0;
            diagnosticPage.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            diagnosticPage.contentContainer.style.flexShrink = 0;

            toolbar = new Toolbar { name = "uss-navigator-inspector-tabs" };
            toolbar.style.flexShrink = 0;
            toolbar.style.height = 26;
            propertiesTab = new ToolbarToggle { name = "uss-navigator-properties-tab" };
            diagnosticsTab = new ToolbarToggle { name = "uss-navigator-diagnostics-tab" };
            propertiesTab.style.flexGrow = diagnosticsTab.style.flexGrow = 1;
            propertiesTab.style.fontSize = diagnosticsTab.style.fontSize = 12;
            propertiesTab.style.unityTextAlign = diagnosticsTab.style.unityTextAlign = UnityEngine.TextAnchor.MiddleCenter;
            propertiesTab.RegisterValueChangedCallback(evt => ShowDiagnostics(false));
            diagnosticsTab.RegisterValueChangedCallback(evt => ShowDiagnostics(true));
            toolbar.Add(propertiesTab);
            toolbar.Add(diagnosticsTab);
            languageMenu = new ToolbarMenu { name = "uss-navigator-language-menu" };
            languageMenu.style.minWidth = 78;
            languageMenu.style.flexShrink = 0;
            languageMenu.menu.AppendAction("中文", _ => L10n.SetLanguage("zh"),
                _ => L10n.Language == "zh" ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);
            languageMenu.menu.AppendAction("English", _ => L10n.SetLanguage("en"),
                _ => L10n.Language == "en" ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);
            toolbar.Add(languageMenu);
            inspector.Add(toolbar);
            inspector.Add(nativePage);
            inspector.Add(diagnosticPage);
            DiagnosticsVisible = SessionState.GetBool(SessionKey, false);
            ApplyVisibility();
            RefreshLanguage();
        }

        /// <summary>仅更新文字，保留原生控件、当前页签、焦点和两页的滚动位置。</summary>
        internal void RefreshLanguage()
        {
            propertiesTab.text = L10n.Tr("属性", "Properties");
            diagnosticsTab.text = L10n.Tr("诊断", "Diagnostics");
            propertiesTab.tooltip = L10n.Tr("Unity 原生属性与样式编辑", "Unity's native property and style editing");
            diagnosticsTab.tooltip = L10n.Tr("只读盒模型、父子布局对照与样式来源",
                "Read-only box model, parent/element layout comparison and style sources");
            languageMenu.text = L10n.Language == "zh" ? "中文" : "English";
            languageMenu.tooltip = L10n.Tr("扩展界面语言", "Extension interface language");
        }

        /// <summary>只切换页容器，保留两页的控件、折叠和滚动状态；事件交给会话清理悬停高亮。</summary>
        internal void ShowDiagnostics(bool value)
        {
            // 再次点击当前页也保持单选，不能把两个 ToolbarToggle 都取消。
            propertiesTab.SetValueWithoutNotify(!value);
            diagnosticsTab.SetValueWithoutNotify(value);
            if (DiagnosticsVisible == value) return;
            DiagnosticsVisible = value;
            SessionState.SetBool(SessionKey, value);
            ApplyVisibility();
            PageChanged?.Invoke();
        }

        void ApplyVisibility()
        {
            propertiesTab.SetValueWithoutNotify(!DiagnosticsVisible);
            diagnosticsTab.SetValueWithoutNotify(DiagnosticsVisible);
            nativePage.style.display = DiagnosticsVisible ? DisplayStyle.None : DisplayStyle.Flex;
            diagnosticPage.style.display = DiagnosticsVisible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void Dispose()
        {
            toolbar.RemoveFromHierarchy();
            diagnosticPage.RemoveFromHierarchy();
            // 原生窗口可能已在关闭/重建。只在原包装仍属于该 Inspector 时恢复其子节点，
            // 避免将旧窗口的控件插入 Unity 新建的视觉树；从未改过原生控件的内联样式。
            if (nativePage.hierarchy.parent == inspector)
            {
                int index = inspector.hierarchy.IndexOf(nativePage);
                foreach (VisualElement child in nativePage.hierarchy.Children().ToArray())
                    inspector.hierarchy.Insert(index++, child);
            }
            nativePage.RemoveFromHierarchy();
        }
    }
}
