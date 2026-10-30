using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace UIBuilderUssNavigator
{
    /// <summary>
    /// 盒模型内的父子布局对照。左列为 hierarchy.parent 实际布局父级，右列为当前元素；只读 live 数据，
    /// 不根据 Grow/Shrink 等条件猜测布局根因。尺寸是布局结果，Basis 和 min/max 保留原生单位。
    /// 整张表共用一个悬停区域，具体覆盖层所有权交给 InspectorBoxModel 统一管理。
    /// </summary>
    internal sealed class InspectorLayoutComparison
    {
        const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        readonly Foldout root;
        readonly VisualElement table;
        readonly Label hint;
        readonly Label footer;
        readonly Label captionHeading;
        readonly Label parentHeading;
        readonly Label currentHeading;
        readonly List<Row> rows = new List<Row>();
        readonly FieldInfo computedStyle;
        readonly Dictionary<string, PropertyInfo> lengthProperties = new Dictionary<string, PropertyInfo>();
        bool darkSkin = EditorGUIUtility.isProSkin;
        VisualElement inspectedElement;

        internal VisualElement Element => root;

        internal InspectorLayoutComparison(Action<bool> hover)
        {
            // IResolvedStyle 的 StyleFloat 接口不能保留百分比单位。读取原生 Length，避免把 50% 写成 50px。
            computedStyle = typeof(VisualElement).GetField("m_Style", Members) ??
                throw new NotSupportedException(L10n.Tr("布局对照接口不兼容：VisualElement.m_Style",
                    "Layout comparison API is incompatible: VisualElement.m_Style"));
            foreach (string name in new[] { "flexBasis", "minWidth", "minHeight", "maxWidth", "maxHeight" })
                lengthProperties.Add(name, computedStyle.FieldType.GetProperty(name, Members) ??
                    throw new NotSupportedException(L10n.Tr("布局对照接口不兼容：ComputedStyle.",
                        "Layout comparison API is incompatible: ComputedStyle.") + name));

            root = new Foldout { name = "box-model-layout-comparison", value = false };
            root.style.flexShrink = 0;
            root.style.marginTop = 5;
            hint = Text(string.Empty, "layout-comparison-hint");
            hint.style.marginBottom = 5;
            root.Add(hint);
            table = new VisualElement { name = "layout-comparison-table" };
            table.style.flexShrink = 0;
            var header = new VisualElement { pickingMode = PickingMode.Ignore };
            header.style.flexDirection = FlexDirection.Row;
            captionHeading = Cell(string.Empty, true);
            header.Add(captionHeading);
            parentHeading = Cell(string.Empty, false);
            currentHeading = Cell(string.Empty, false);
            parentHeading.style.color = EditorGUIUtility.isProSkin ? CanvasBoxModelOverlay.ParentLayoutColor : (Color)new Color32(103, 74, 151, 255);
            currentHeading.style.color = EditorGUIUtility.isProSkin ? CanvasBoxModelOverlay.CurrentLayoutColor : (Color)new Color32(44, 100, 151, 255);
            parentHeading.style.fontSize = currentHeading.style.fontSize = 13;
            parentHeading.style.unityFontStyleAndWeight = currentHeading.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.Add(parentHeading);
            header.Add(currentHeading);
            table.Add(header);
            Add("元素", "Element", "identity", element => StyleSourceReader.Describe(element));
            Add("外框", "Border box", "border-size", element => Size(element.layout));
            Add("内容区", "Content box", "content-size", element => Size(element.contentRect));
            Add("排列方向", "Flex direction", "flex-direction", element => CssEnum(element.resolvedStyle.flexDirection));
            Add("换行", "Flex wrap", "flex-wrap", element => CssEnum(element.resolvedStyle.flexWrap));
            Add("主轴对齐", "Justify content", "justify-content", element => CssEnum(element.resolvedStyle.justifyContent));
            Add("交叉轴对齐", "Align items", "align-items", element => CssEnum(element.resolvedStyle.alignItems));
            Add("多行对齐", "Align content", "align-content", element => CssEnum(element.resolvedStyle.alignContent));
            Add("Grow", "Grow", "flex-grow", element => InspectorBoxModel.Format(element.resolvedStyle.flexGrow));
            Add("Shrink", "Shrink", "flex-shrink", element => InspectorBoxModel.Format(element.resolvedStyle.flexShrink));
            Add("Basis", "Basis", "flex-basis", element => LengthValue(element, "flexBasis"));
            Add("自身对齐", "Align self", "align-self", element => CssEnum(element.resolvedStyle.alignSelf));
            Add("定位", "Position", "position", element => CssEnum(element.resolvedStyle.position));
            Add("最小宽度", "Min width", "min-width", element => LengthValue(element, "minWidth"));
            Add("最大宽度", "Max width", "max-width", element => LengthValue(element, "maxWidth"));
            Add("最小高度", "Min height", "min-height", element => LengthValue(element, "minHeight"));
            Add("最大高度", "Max height", "max-height", element => LengthValue(element, "maxHeight"));
            root.Add(table);
            footer = Text(string.Empty, "layout-comparison-footer");
            footer.style.marginTop = 5;
            root.Add(footer);
            // 所有表内文字和行忽略拾取；在父列/当前列之间移动不会产生反复的 enter/leave。
            table.RegisterCallback<PointerEnterEvent>(evt => hover(true));
            table.RegisterCallback<PointerMoveEvent>(evt => hover(true));
            table.RegisterCallback<PointerLeaveEvent>(evt => hover(false));
            root.RegisterValueChangedCallback(evt => { if (!evt.newValue) hover(false); });
            root.RegisterCallback<DetachFromPanelEvent>(evt => hover(false));
            RefreshLanguage();
        }

        internal void RefreshLanguage()
        {
            root.text = L10n.Tr("父容器 ↔ 当前元素（布局）", "Parent ↔ Current Element (Layout)");
            hint.text = L10n.Tr(
                "悬停表格：紫色为父内容区，蓝色为当前外框。外框/内容区是布局结果（px），Basis 和 min/max 保留样式单位。",
                "Hover the table: purple is the parent content box; blue is the current border box. Box sizes are computed in px; Basis and min/max keep their style units.");
            footer.text = L10n.Tr("显示实际参与布局的父容器；ScrollView 的内容容器可能与逻辑父级不同。",
                "Shows the actual layout parent; a ScrollView content container may differ from the logical parent.");
            captionHeading.text = L10n.Tr("布局属性", "Layout property");
            parentHeading.text = L10n.Tr("父容器", "Parent");
            currentHeading.text = L10n.Tr("当前元素", "Current");
            // 英文属性名更长，略增首列宽度；两种语言仍在同一表格中自适应，不重建行。
            captionHeading.style.width = L10n.Language == "en" ? 105 : 86;
            TintComment(hint);
            TintComment(footer);
            foreach (Row row in rows)
            {
                row.Caption.text = DiagnosticText.Colorize(L10n.Tr(row.CaptionZh, row.CaptionEn),
                    DiagnosticTone.Property, EditorGUIUtility.isProSkin);
                row.Caption.style.width = L10n.Language == "en" ? 105 : 86;
            }
            Refresh(inspectedElement);
        }

        internal void Refresh(VisualElement element)
        {
            inspectedElement = element;
            if (darkSkin != EditorGUIUtility.isProSkin)
            {
                darkSkin = EditorGUIUtility.isProSkin;
                TintComment(hint);
                TintComment(footer);
                parentHeading.style.color = darkSkin ? CanvasBoxModelOverlay.ParentLayoutColor : (Color)new Color32(103, 74, 151, 255);
                currentHeading.style.color = darkSkin ? CanvasBoxModelOverlay.CurrentLayoutColor : (Color)new Color32(44, 100, 151, 255);
            }
            bool available = element != null && element.panel != null;
            root.style.display = available ? DisplayStyle.Flex : DisplayStyle.None;
            if (!available || !root.value) return;
            VisualElement parent = element.hierarchy.parent;
            foreach (Row row in rows)
            {
                row.Caption.text = DiagnosticText.Colorize(L10n.Tr(row.CaptionZh, row.CaptionEn),
                    DiagnosticTone.Property, EditorGUIUtility.isProSkin);
                row.Parent.SetValue(parent == null ? L10n.Tr("无父容器", "No parent") : row.Read(parent));
                row.Current.SetValue(row.Read(element));
            }
        }

        string LengthValue(VisualElement element, string property) =>
            lengthProperties[property].GetValue(computedStyle.GetValue(element)).ToString().ToLowerInvariant();

        void Add(string captionZh, string captionEn, string name, Func<VisualElement, string> read)
        {
            var line = new VisualElement { name = "layout-comparison-" + name, pickingMode = PickingMode.Ignore };
            line.style.flexDirection = FlexDirection.Row;
            line.style.flexShrink = 0;
            line.style.borderBottomWidth = 1;
            line.style.borderBottomColor = new Color(0.5f, 0.5f, 0.5f, 0.18f);
            Label captionLabel = Cell(L10n.Tr(captionZh, captionEn), true);
            captionLabel.enableRichText = true;
            line.Add(captionLabel);
            Func<string, bool, string> format = name == "identity" ? DiagnosticText.Identity : DiagnosticText.Value;
            var parent = ValueCell("layout-parent-" + name, format);
            var current = ValueCell("layout-current-" + name, format);
            parent.TextLabel.tooltip = current.TextLabel.tooltip = name;
            line.Add(parent);
            line.Add(current);
            table.Add(line);
            rows.Add(new Row { Caption = captionLabel, CaptionZh = captionZh, CaptionEn = captionEn,
                Parent = parent, Current = current, Read = read });
        }

        static DiagnosticValue ValueCell(string name, Func<string, bool, string> format)
        {
            var value = new DiagnosticValue(name, format);
            value.style.flexBasis = 0;
            value.style.flexGrow = 1;
            value.style.marginTop = value.style.marginBottom = 3;
            value.style.marginRight = 5;
            return value;
        }

        static Label Cell(string text, bool caption)
        {
            Label label = Text(text, null);
            label.style.marginTop = label.style.marginBottom = 3;
            label.style.marginRight = 5;
            if (caption) label.style.width = 86;
            else
            {
                label.style.flexBasis = 0;
                label.style.flexGrow = 1;
                label.style.minWidth = 0;
            }
            return label;
        }

        static Label Text(string text, string name)
        {
            var label = new Label(text) { name = name, pickingMode = PickingMode.Ignore, enableRichText = false };
            label.style.fontSize = 12;
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.marginLeft = label.style.marginRight = 0;
            TintComment(label);
            return label;
        }

        static void TintComment(Label label)
        {
            ColorUtility.TryParseHtmlString("#" + DiagnosticText.ColorCode(DiagnosticTone.Comment, EditorGUIUtility.isProSkin), out Color color);
            label.style.color = color;
        }

        static string Size(Rect rect) => InspectorBoxModel.Format(rect.width) + " × " + InspectorBoxModel.Format(rect.height) + " px";
        static string CssEnum(object value) => value.ToString().Replace("Reverse", "-reverse")
            .Replace("Flex", "flex-").Replace("Space", "space-").ToLowerInvariant();

        sealed class Row
        {
            internal Label Caption;
            internal string CaptionZh;
            internal string CaptionEn;
            internal DiagnosticValue Parent;
            internal DiagnosticValue Current;
            internal Func<VisualElement, string> Read;
        }
    }
}
