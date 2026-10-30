using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine.UIElements;

namespace UIBuilderUssNavigator
{
    /// <summary>
    /// 在页面元素的原生右键菜单中列出相关 USS 规则，并沿用 Builder 的活动样式表与选择通知。
    /// 匹配使用 Unity 的本地样式匹配器（包含 hover/focus 等状态规则），不自行解释选择器文本。
    /// </summary>
    internal sealed class RelatedStylesMenu : IDisposable
    {
        const BindingFlags Members = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        readonly EditorWindow window;
        readonly BuilderAccess access;
        readonly Action<VisualElement, VisualElement> revealSelection;
        readonly MethodInfo matchingSelectors;
        readonly FieldInfo matchSheet;
        readonly FieldInfo matchSelector;
        readonly MethodInfo selectorProperty;
        readonly MethodInfo selectorText;
        readonly MethodInfo selectorContainer;
        readonly PropertyInfo documentRoot;
        readonly PropertyInfo selection;
        readonly MethodInfo select;
        readonly MethodInfo setActiveUss;
        readonly StyleRuleListLocator ruleList;

        internal sealed class Entry
        {
            internal string Label;
            internal StyleSheet Sheet;
            internal object Selector;
            internal bool CanNavigate;
            internal VisualElement Element;
        }

        internal RelatedStylesMenu(EditorWindow window, BuilderAccess access, Action<VisualElement, VisualElement> revealSelection)
        {
            this.window = window;
            this.access = access;
            this.revealSelection = revealSelection;
            Type builder = window.GetType();
            Assembly assembly = builder.Assembly;
            Type shared = assembly.GetType("Unity.UI.Builder.BuilderSharedStyles", true);
            matchingSelectors = Required(shared.GetMethod("GetMatchingSelectorsOnElementFromLocalStyleSheet", Members),
                "BuilderSharedStyles.GetMatchingSelectorsOnElementFromLocalStyleSheet");
            Type matchType = matchingSelectors.ReturnType.GetGenericArguments()[0];
            matchSheet = Required(matchType.GetField("sheet", Members), "SelectorMatchRecord.sheet");
            matchSelector = Required(matchType.GetField("complexSelector", Members), "SelectorMatchRecord.complexSelector");
            selectorProperty = Required(shared.GetMethod("GetSelectorProperty", Members), "BuilderSharedStyles.GetSelectorProperty");
            selectorContainer = Required(shared.GetMethod("GetSelectorContainerElement", Members), "BuilderSharedStyles.GetSelectorContainerElement");
            Type exporter = assembly.GetType("Unity.UI.Builder.BuilderStyleSheetExporter", true);
            selectorText = Required(exporter.GetMethod("GetSelectorString", Members), "BuilderStyleSheetExporter.GetSelectorString");
            documentRoot = Required(builder.GetProperty("documentRootElement", Members), "Builder.documentRootElement");
            selection = Required(builder.GetProperty("selection", Members), "Builder.selection");
            select = Required(selection.PropertyType.GetMethod("Select", Members), "BuilderSelection.Select");
            Type utilities = assembly.GetType("Unity.UI.Builder.BuilderStyleSheetsUtilities", true);
            setActiveUss = Required(utilities.GetMethod("SetActiveUSS", Members), "BuilderStyleSheetsUtilities.SetActiveUSS");
            ruleList = new StyleRuleListLocator(window);
            // 在原生菜单构建前补充项目，保留 Unity 原有的复制、删除、重命名等菜单及事件传播。
            window.rootVisualElement.RegisterCallback<ContextualMenuPopulateEvent>(Populate, TrickleDown.TrickleDown);
        }

        void Populate(ContextualMenuPopulateEvent evt)
        {
            VisualElement element = access.GetDocumentElement(evt.target as VisualElement);
            if (element == null) return;
            List<Entry> entries = GetEntries(element);
            if (entries.Count == 0)
            {
                evt.menu.AppendAction(L10n.Tr("相关样式/没有匹配的 USS 选择器",
                    "Related Styles/No matching USS selectors"), _ => { }, DropdownMenuAction.Status.Disabled);
                return;
            }
            foreach (Entry entry in entries)
                evt.menu.AppendAction(L10n.Tr("相关样式/", "Related Styles/") + entry.Label, _ => Navigate(entry),
                    entry.CanNavigate ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
        }

        /// <summary>
        /// 仅在打开菜单时计算。用原生 selector 对象去重和定位，保留不同文件、不同位置的同名规则。
        /// Unity 默认/编辑器主题由原生本地匹配器排除；未出现在当前文档样式树的规则保留只读项。
        /// </summary>
        internal List<Entry> GetEntries(VisualElement element)
        {
            Dictionary<object, VisualElement> nodes = GetSelectorNodes();
            var entries = new List<Entry>();
            var seen = new HashSet<object>();
            var labels = new Dictionary<string, int>();
            foreach (object record in (IEnumerable)matchingSelectors.Invoke(null, new object[] { element }))
            {
                object selector = matchSelector.GetValue(record);
                if (!seen.Add(selector)) continue;
                var sheet = (StyleSheet)matchSheet.GetValue(record);
                string text = (string)selectorText.Invoke(null, new[] { selector });
                string path = AssetDatabase.GetAssetPath(sheet);
                string file = path.Length == 0 ? sheet.name : Path.GetFileName(path);
                string label = text + "  [" + file + "]";
                labels.TryGetValue(label, out int previous);
                labels[label] = previous + 1;
                if (previous > 0) label += "  #" + (previous + 1);
                bool found = nodes.ContainsKey(selector);
                // 菜单用 / 分隔子级，显示时转成视觉相近的字符；跳转始终使用原始对象身份。
                entries.Add(new Entry
                {
                    Label = label.Replace('/', '∕') + (found ? string.Empty : L10n.Tr("（只读）", " (read-only)")),
                    Sheet = sheet,
                    Selector = selector,
                    Element = element,
                    CanNavigate = found
                });
            }
            return entries;
        }

        Dictionary<object, VisualElement> GetSelectorNodes()
        {
            var nodes = new Dictionary<object, VisualElement>();
            var root = (VisualElement)documentRoot.GetValue(window);
            // 选择器树与页面视觉树是兄弟节点，不能在 documentRoot 的子树里寻找。
            var container = (VisualElement)selectorContainer.Invoke(null, new object[] { root });
            foreach (VisualElement element in container.Query<VisualElement>().ToList())
            {
                object selector = selectorProperty.Invoke(null, new object[] { element });
                if (selector != null) nodes[selector] = element;
            }
            return nodes;
        }

        internal void Navigate(Entry entry)
        {
            object builderSelection = selection.GetValue(window);
            // 相关样式是对当前元素的辅助查看，不能把 Inspector 的选择切换成样式规则。
            bool sheetChanged = access.ActiveStyleSheet != entry.Sheet;
            if (sheetChanged)
                setActiveUss.Invoke(null, new object[] { builderSelection, window, entry.Sheet });
            if (sheetChanged || !ReferenceEquals(access.SelectedElement, entry.Element))
                select.Invoke(builderSelection, new object[] { null, entry.Element });
            // 切换活动 USS 后重新取得显示节点，不持有上一次菜单构建时的视觉树行。
            VisualElement node = GetSelectorNodes()[entry.Selector];
            ruleList.Reveal(node);
            revealSelection(entry.Element, node);
        }

        /// <summary>来源面板复用同一导航路径；即使规则来自祖先，也保留正在检查的子元素。</summary>
        internal void NavigateRule(VisualElement element, StyleSheet sheet, object selector)
        {
            Navigate(new Entry { Element = element, Sheet = sheet, Selector = selector, CanNavigate = true });
        }

        /// <summary>一次刷新共享当前文档的可导航规则集合；主题和外部模板规则保留只读显示。</summary>
        internal HashSet<object> GetNavigableSelectors() => new HashSet<object>(GetSelectorNodes().Keys);

        internal void ClearRuleMarker() => ruleList.Clear();
        internal void UpdateLayout() => ruleList.UpdateLayout();

        public void Dispose()
        {
            window.rootVisualElement.UnregisterCallback<ContextualMenuPopulateEvent>(Populate, TrickleDown.TrickleDown);
            ruleList.Dispose();
        }

        static T Required<T>(T member, string name) where T : MemberInfo =>
            member ?? throw new NotSupportedException(L10n.Tr("UI Builder 内部接口不兼容：",
                "Incompatible UI Builder internal API: ") + name);
    }
}
