using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UIBuilderUssNavigator
{
    /// <summary>
    /// 在原生 StyleSheets 树中展开、滚动并标出正在查看的规则，不改变 TreeView/Builder 的选择。
    /// 标记是忽略鼠标的独立覆盖层，按虚拟行的双向绑定更新位置，不给复用行留下样式。
    /// </summary>
    internal sealed class StyleRuleListLocator : IDisposable
    {
        const BindingFlags Members = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        readonly object hierarchy;
        readonly TreeView tree;
        readonly ToolbarSearchField search;
        readonly PropertyInfo rootItems;
        readonly MethodInfo findElement;
        readonly MethodInfo expandParents;
        readonly MethodInfo elementProperty;
        readonly PropertyName rowLink;
        readonly PropertyName dataLink;
        readonly VisualElement marker;
        VisualElement target;
        bool pendingScroll;

        internal StyleRuleListLocator(EditorWindow window)
        {
            Type builder = window.GetType();
            object pane = Required(builder.GetProperty("styleSheets", Members), "Builder.styleSheets").GetValue(window);
            Type explorer = pane.GetType().BaseType;
            hierarchy = Required(explorer.GetField("m_ElementHierarchyView", Members), "BuilderExplorer.m_ElementHierarchyView").GetValue(pane);
            search = (ToolbarSearchField)Required(explorer.GetField("m_SearchField", Members), "BuilderExplorer.m_SearchField").GetValue(pane);
            Type view = hierarchy.GetType();
            tree = (TreeView)Required(view.GetProperty("treeView", Members), "ElementHierarchyView.treeView").GetValue(hierarchy);
            rootItems = Required(view.GetProperty("treeRootItems", Members), "ElementHierarchyView.treeRootItems");
            findElement = Required(view.GetMethod("FindElement", Members), "ElementHierarchyView.FindElement");
            expandParents = Required(view.GetMethod("RecursivelyExpandToItem", Members), "ElementHierarchyView.RecursivelyExpandToItem");
            elementProperty = Required(typeof(VisualElement).GetMethod("GetProperty", Members), "VisualElement.GetProperty");
            Type constants = builder.Assembly.GetType("Unity.UI.Builder.BuilderConstants", true);
            rowLink = new PropertyName((string)Required(constants.GetField("ElementLinkedExplorerItemVEPropertyName", Members),
                "BuilderConstants.ElementLinkedExplorerItemVEPropertyName").GetValue(null));
            dataLink = new PropertyName((string)Required(constants.GetField("ElementLinkedDocumentVisualElementVEPropertyName", Members),
                "BuilderConstants.ElementLinkedDocumentVisualElementVEPropertyName").GetValue(null));

            marker = new VisualElement { name = "uss-navigator-rule-marker", pickingMode = PickingMode.Ignore };
            marker.style.position = Position.Absolute;
            marker.style.backgroundColor = new Color(0.22f, 0.48f, 0.85f, 0.22f);
            marker.style.borderLeftWidth = 3;
            marker.style.borderLeftColor = new Color(0.3f, 0.65f, 1f);
            marker.style.display = DisplayStyle.None;
        }

        internal void Reveal(VisualElement selector)
        {
            target = selector;
            // 仅当搜索确实挡住目标时清除它，让正常筛选结果及用户浏览状态尽量保留。
            if (!ReferenceEquals(FindTarget().data, target) && search.value.Length > 0)
                search.value = string.Empty;
            pendingScroll = true;
            UpdateLayout();
        }

        TreeViewItemData<VisualElement> FindTarget() =>
            (TreeViewItemData<VisualElement>)findElement.Invoke(hierarchy, new object[] { rootItems.GetValue(hierarchy), target });

        /// <summary>由现有 Editor update 驱动。定位只消费一次；之后标记跟随行滚动，不拉回用户视图。</summary>
        internal void UpdateLayout()
        {
            if (target == null) return;
            if (pendingScroll)
            {
                TreeViewItemData<VisualElement> item = FindTarget();
                if (!ReferenceEquals(item.data, target))
                {
                    marker.style.display = DisplayStyle.None;
                    return; // 搜索或树重建尚未完成，等待原生布局流程。
                }
                expandParents.Invoke(hierarchy, new object[] { target.parent });
                tree.ScrollToItemById(item.id);
                pendingScroll = false;
            }

            var row = elementProperty.Invoke(target, new object[] { rowLink }) as VisualElement;
            VisualElement viewport = tree.Q<ScrollView>().contentViewport;
            // 离开可见区时行会被解绑/复用；检查反向链接，避免把蓝框留到另一条规则上。
            if (row == null || row.panel == null ||
                !ReferenceEquals(elementProperty.Invoke(row, new object[] { dataLink }), target) ||
                !row.worldBound.Overlaps(viewport.worldBound))
            {
                marker.style.display = DisplayStyle.None;
                return;
            }
            if (marker.parent != viewport) viewport.Add(marker);
            Rect bounds = viewport.WorldToLocal(row.worldBound);
            marker.style.left = bounds.x;
            marker.style.top = bounds.y;
            marker.style.width = bounds.width;
            marker.style.height = bounds.height;
            marker.style.display = DisplayStyle.Flex;
        }

        internal void Clear()
        {
            target = null;
            pendingScroll = false;
            marker.style.display = DisplayStyle.None;
        }

        public void Dispose() => marker.RemoveFromHierarchy();

        static T Required<T>(T member, string name) where T : MemberInfo =>
            member ?? throw new NotSupportedException(L10n.Tr("UI Builder 内部接口不兼容：",
                "Incompatible UI Builder internal API: ") + name);
    }
}
