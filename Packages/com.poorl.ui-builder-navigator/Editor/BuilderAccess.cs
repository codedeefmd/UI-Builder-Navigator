using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine.UIElements;

namespace UIBuilderUssNavigator
{
    /// <summary>
    /// 集中读取 Unity 6.3 UI Builder 的内部选择状态和完整 USS 预览。
    /// Unity 没有公开代码预览扩展接口，因此这里使用反射；不修改它的程序集、文档或选择结果。
    /// 内部成员变化时明确报出不兼容，由插件入口停止联动并恢复原生面板，不尝试猜测替代成员。
    /// </summary>
    internal sealed class BuilderAccess
    {
        const BindingFlags Members = BindingFlags.Instance | BindingFlags.Static |
                                     BindingFlags.Public | BindingFlags.NonPublic;

        readonly EditorWindow window;
        readonly PropertyInfo selection;
        readonly MethodInfo firstSelected;
        readonly FieldInfo ussPreview;
        readonly FieldInfo uxmlPreview;
        readonly PropertyInfo inspector;
        readonly PropertyInfo viewport;
        readonly FieldInfo viewportArea;
        readonly MethodInfo pickViewportElement;
        readonly PropertyInfo previewEnabled;
        readonly PropertyInfo document;
        readonly PropertyInfo activeStyleSheet;
        readonly PropertyInfo activeUxml;
        readonly PropertyInfo uxmlText;
        readonly MethodInfo targetUss;
        readonly PropertyInfo previewText;
        readonly MethodInfo selectorProperty;
        readonly MethodInfo selectorString;
        readonly PropertyInfo selectorRule;
        readonly MethodInfo elementProperty;
        readonly UnityEngine.PropertyName explorerElementLink;
        readonly UnityEngine.PropertyName elementAssetLink;
        readonly UnityEngine.PropertyName treeAssetLink;
        readonly PropertyInfo documentTree;
        readonly FieldInfo rootAsset;
        readonly PropertyInfo assetParent;
        readonly PropertyInfo assetOwner;
        readonly PropertyInfo assetChildCount;
        readonly PropertyInfo assetChild;
        readonly PropertyInfo assetTypeName;

        internal BuilderAccess(EditorWindow window)
        {
            this.window = window;
            Type builderType = window.GetType();
            selection = Required(builderType.GetProperty("selection", Members), "Builder.selection");
            firstSelected = Required(selection.PropertyType.GetMethod("GetFirstSelectedElement", Members),
                "BuilderSelection.GetFirstSelectedElement");
            ussPreview = Required(builderType.GetField("m_UssPreview", Members), "Builder.m_UssPreview");
            uxmlPreview = Required(builderType.GetField("m_UxmlPreview", Members), "Builder.m_UxmlPreview");
            inspector = Required(builderType.GetProperty("inspector", Members), "Builder.inspector");
            viewport = Required(builderType.GetProperty("viewport", Members), "Builder.viewport");
            viewportArea = Required(viewport.PropertyType.GetField("m_Viewport", Members), "BuilderViewport.m_Viewport");
            pickViewportElement = Required(viewport.PropertyType.GetMethod("PickElement", Members), "BuilderViewport.PickElement");
            previewEnabled = Required(viewport.PropertyType.GetProperty("isPreviewEnabled", Members), "BuilderViewport.isPreviewEnabled");
            document = Required(builderType.GetProperty("document", Members), "Builder.document");
            activeStyleSheet = Required(document.PropertyType.GetProperty("activeStyleSheet", Members), "BuilderDocument.activeStyleSheet");
            activeUxml = Required(document.PropertyType.GetProperty("activeOpenUXMLFile", Members),
                "BuilderDocument.activeOpenUXMLFile");
            uxmlText = Required(activeUxml.PropertyType.GetProperty("uxmlPreview", Members),
                "BuilderDocumentOpenUXML.uxmlPreview");
            targetUss = Required(ussPreview.FieldType.GetMethod("GetTargetUss", Members),
                "BuilderUssPreview.GetTargetUss");
            previewText = Required(targetUss.ReturnType.GetProperty("ussPreview", Members),
                "BuilderDocumentOpenUSS.ussPreview");

            Type sharedStyles = builderType.Assembly.GetType("Unity.UI.Builder.BuilderSharedStyles", true);
            selectorProperty = Required(sharedStyles.GetMethod("GetSelectorProperty", Members),
                "BuilderSharedStyles.GetSelectorProperty");
            selectorString = Required(sharedStyles.GetMethod("GetSelectorString", Members),
                "BuilderSharedStyles.GetSelectorString");
            selectorRule = Required(selectorProperty.ReturnType.GetProperty("rule", Members),
                "StyleComplexSelector.rule");
            elementProperty = Required(typeof(VisualElement).GetMethod("GetProperty", Members),
                "VisualElement.GetProperty");
            Type constants = builderType.Assembly.GetType("Unity.UI.Builder.BuilderConstants", true);
            FieldInfo linkName = Required(constants.GetField("ExplorerItemElementLinkVEPropertyName", Members),
                "BuilderConstants.ExplorerItemElementLinkVEPropertyName");
            explorerElementLink = new UnityEngine.PropertyName((string)linkName.GetValue(null));
            elementAssetLink = new UnityEngine.PropertyName((string)Required(constants.GetField(
                "ElementLinkedVisualElementAssetVEPropertyName", Members), "BuilderConstants.ElementLinkedVisualElementAssetVEPropertyName").GetValue(null));
            treeAssetLink = new UnityEngine.PropertyName((string)Required(constants.GetField(
                "ElementLinkedVisualTreeAssetVEPropertyName", Members), "BuilderConstants.ElementLinkedVisualTreeAssetVEPropertyName").GetValue(null));
            documentTree = Required(document.PropertyType.GetProperty("visualTreeAsset", Members), "BuilderDocument.visualTreeAsset");
            rootAsset = Required(typeof(VisualTreeAsset).GetField("m_VisualTree", Members), "VisualTreeAsset.m_VisualTree");
            assetParent = Required(rootAsset.FieldType.GetProperty("parentAsset", Members), "UxmlAsset.parentAsset");
            Type assetType = assetParent.PropertyType;
            assetOwner = Required(assetType.GetProperty("visualTreeAsset", Members), "UxmlAsset.visualTreeAsset");
            assetChildCount = Required(assetType.GetProperty("childCount", Members), "UxmlAsset.childCount");
            assetChild = Required(assetType.GetProperty("Item", Members), "UxmlAsset.Item");
            assetTypeName = Required(assetType.GetProperty("fullTypeName", Members), "UxmlAsset.fullTypeName");
        }

        internal VisualElement Preview => (VisualElement)ussPreview.GetValue(window);
        internal VisualElement UxmlPreview => (VisualElement)uxmlPreview.GetValue(window);
        internal VisualElement Inspector => (VisualElement)inspector.GetValue(window);
        internal VisualElement Viewport => (VisualElement)viewportArea.GetValue(viewport.GetValue(window));
        internal bool IsPreviewEnabled => (bool)previewEnabled.GetValue(viewport.GetValue(window));

        /// <summary>沿用原生文档拾取，排除编辑器覆盖层，并保留模板实例映射到当前文档节点的行为。</summary>
        internal VisualElement PickViewportElement(UnityEngine.Vector2 panelPosition) =>
            (VisualElement)pickViewportElement.Invoke(viewport.GetValue(window), new object[] { panelPosition, null });
        internal StyleSheet ActiveStyleSheet => (StyleSheet)activeStyleSheet.GetValue(document.GetValue(window));

        /// <summary>与 USS 一样读取完整内存预览，不受原生 TextField 的截断影响，也包含未保存修改。</summary>
        internal string FullUxmlText
        {
            get
            {
                object openUxml = activeUxml.GetValue(document.GetValue(window));
                return openUxml == null ? string.Empty : (string)uxmlText.GetValue(openUxml);
            }
        }

        internal VisualElement SelectedElement =>
            (VisualElement)firstSelected.Invoke(selection.GetValue(window), null);

        /// <summary>
        /// 读取内存中的完整导出文本，包含尚未保存的修改。
        /// 原生 TextField 已经截断的显示字符串不能用来定位文件后半部分的规则。
        /// 没有活动样式表是正常的窗口状态，此时返回空文本。
        /// </summary>
        internal string FullPreviewText
        {
            get
            {
                object openUss = targetUss.Invoke(Preview, null);
                return openUss == null ? string.Empty : (string)previewText.GetValue(openUss);
            }
        }

        /// <summary>只有样式选择器节点带有对应属性；选择普通 UXML 元素时不触发定位。</summary>
        internal string GetSelector(VisualElement element)
        {
            object selector = selectorProperty.Invoke(null, new object[] { element });
            return selector == null ? null : (string)selectorString.Invoke(null, new object[] { element });
        }

        /// <summary>样式列表显示行与实际选择器节点分开存放；通过原生行链接判断重复点击同一条规则。</summary>
        internal bool IsSelectorItem(VisualElement element)
        {
            var linked = elementProperty.Invoke(element, new object[] { explorerElementLink }) as VisualElement;
            return linked != null && GetSelector(linked) != null;
        }

        /// <summary>Hierarchy 行链接或画布中的文档节点；不把预览 Label 等编辑器自身控件当作页面元素。</summary>
        internal bool IsUxmlElementItem(VisualElement element)
        {
            var linked = ReadElementProperty(element, explorerElementLink) as VisualElement;
            VisualElement candidate = linked ?? element;
            return ReadElementProperty(candidate, elementAssetLink) != null ||
                ReadElementProperty(candidate, treeAssetLink) is VisualTreeAsset;
        }

        /// <summary>将 Hierarchy 的显示行或画布节点还原为页面元素，供右键菜单使用。</summary>
        internal VisualElement GetDocumentElement(VisualElement target)
        {
            for (VisualElement element = target; element != null; element = element.parent)
            {
                var linked = ReadElementProperty(element, explorerElementLink) as VisualElement;
                if (linked != null)
                    return GetSelector(linked) == null && GetUxmlElementPath(linked, out _) != null ? linked : null;
                if (ReadElementProperty(element, elementAssetLink) != null)
                    return GetUxmlElementPath(element, out _) != null ? element : null;
            }
            return null;
        }

        /// <summary>
        /// 用序列化节点的父子索引定位源码，不依赖可重复/缺失的 name 或 class。
        /// 内部控件子节点和外部模板内容没有当前文档声明，沿视觉父级映射到最近的控件/Instance 标签。
        /// </summary>
        internal List<int> GetUxmlElementPath(VisualElement selected, out string tag)
        {
            tag = null;
            object tree = documentTree.GetValue(document.GetValue(window));
            object root = rootAsset.GetValue(tree);
            object node = null;
            for (VisualElement element = selected; element != null; element = element.parent)
            {
                object candidate = ReadElementProperty(element, elementAssetLink);
                if (candidate != null && ReferenceEquals(assetOwner.GetValue(candidate), tree))
                {
                    node = candidate;
                    break;
                }
                if (ReferenceEquals(element, selected) && ReferenceEquals(ReadElementProperty(element, treeAssetLink), tree))
                {
                    node = root;
                    break;
                }
            }
            if (node == null) return null;
            string typeName = (string)assetTypeName.GetValue(node);
            tag = typeName.Substring(typeName.LastIndexOf('.') + 1);
            var path = new List<int>();
            while (!ReferenceEquals(node, root))
            {
                object parent = assetParent.GetValue(node);
                // 删除/切换文档时，旧选择可能暂时指向已脱离序列化树的节点。
                if (parent == null) return null;
                int count = (int)assetChildCount.GetValue(parent);
                int index = 0;
                while (index < count && !ReferenceEquals(assetChild.GetValue(parent, new object[] { index }), node)) index++;
                if (index == count) return null;
                path.Add(index);
                node = parent;
            }
            path.Reverse();
            return path;
        }

        object ReadElementProperty(VisualElement element, UnityEngine.PropertyName key) =>
            elementProperty.Invoke(element, new object[] { key });

        /// <summary>
        /// 同一个选择器可以在同一张表里出现多次，不能始终跳到第一次。
        /// 只统计当前样式表中位于所选项之前的同名规则；一个逗号分组共享同一条规则，去重后计数。
        /// </summary>
        internal int GetOccurrence(VisualElement selected, string selector)
        {
            object selectedRule = RuleOf(selected);
            var precedingRules = new HashSet<object>();
            foreach (VisualElement sibling in selected.parent.Children())
            {
                if (ReferenceEquals(sibling, selected)) break;
                if (GetSelector(sibling) != selector) continue;
                object rule = RuleOf(sibling);
                if (!ReferenceEquals(rule, selectedRule)) precedingRules.Add(rule);
            }
            return precedingRules.Count;
        }

        object RuleOf(VisualElement element)
        {
            object selector = selectorProperty.Invoke(null, new object[] { element });
            return selectorRule.GetValue(selector);
        }

        static T Required<T>(T member, string name) where T : MemberInfo
        {
            return member ?? throw new NotSupportedException(L10n.Tr("UI Builder 内部接口不兼容：",
                "Incompatible UI Builder internal API: ") + name);
        }
    }
}
