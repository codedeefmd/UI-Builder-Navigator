using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace UIBuilderUssNavigator
{
    /// <summary>
    /// 在 Inspector 诊断页显示当前页面元素的计算盒模型；只读，不写入元素样式或文档。
    /// 图形各层是示意，不按数值缩放；数字来自布局结果，不受 UI Builder 画布缩放影响。
    /// 与现有预览共用刷新周期，覆盖只改变四边间距、但元素外框尺寸未变化的编辑。
    /// </summary>
    internal sealed class InspectorBoxModel : IDisposable
    {
        // 一个 Inspector 入口同一时刻只持有一种高亮，不让迟到的离开事件清掉后续模式。
        enum InspectorHighlight { Margin, Border, Padding, Content, LayoutComparison }
        readonly VisualElement inspector;
        readonly Foldout root;
        readonly VisualElement body;
        readonly InspectorLayoutComparison comparison;
        readonly Label target;
        readonly VisualElement diagram;
        readonly Edges margin;
        readonly Edges border;
        readonly Edges padding;
        readonly Label contentSize;
        readonly Label borderSize;
        readonly VisualElement content;
        readonly CanvasBoxModelOverlay overlay;
        VisualElement inspectedElement;
        InspectorHighlight? hovered;
        bool previewEnabled;

        internal bool IsAttached => root.parent == inspector && root.panel != null && overlay.IsAttached;
        internal CanvasBoxModelOverlay CanvasOverlay => overlay;

        internal InspectorBoxModel(VisualElement inspector, VisualElement viewport)
        {
            this.inspector = inspector;
            root = new Foldout { name = "uss-navigator-box-model", value = true };
            root.style.flexShrink = 0;
            root.style.marginTop = 4;
            root.style.marginBottom = 6;
            root.style.marginLeft = root.style.marginRight = 6;
            root.style.borderTopWidth = 1;
            root.style.borderTopColor = new Color(0.5f, 0.5f, 0.5f, 0.35f);
            // 滚动由诊断页统一承接，盒模型及展开的布局表自然占高，不再嵌套滚动视图。
            body = new VisualElement { name = "box-model-body" };
            body.style.flexShrink = 0;
            root.Add(body);

            target = new Label { name = "box-model-target", enableRichText = true, parseEscapeSequences = false };
            target.style.fontSize = 13;
            target.style.marginBottom = 4;
            target.style.overflow = Overflow.Hidden;
            target.style.textOverflow = TextOverflow.Ellipsis;
            body.Add(target);

            diagram = new VisualElement { name = "box-model-diagram" };
            diagram.style.height = 176;
            diagram.style.flexShrink = 0;
            margin = new Edges(diagram, "margin", CanvasBoxModelOverlay.ColorFor(BoxModelRegion.Margin));
            border = new Edges(Inset(diagram), "border", CanvasBoxModelOverlay.ColorFor(BoxModelRegion.Border));
            padding = new Edges(Inset(border.Element), "padding", CanvasBoxModelOverlay.ColorFor(BoxModelRegion.Padding));
            content = Inset(padding.Element);
            content.style.backgroundColor = CanvasBoxModelOverlay.ColorFor(BoxModelRegion.Content);
            content.style.justifyContent = Justify.Center;
            content.Add(Caption("content"));
            contentSize = Caption(string.Empty);
            contentSize.name = "box-model-content-size";
            contentSize.style.unityFontStyleAndWeight = FontStyle.Bold;
            content.Add(contentSize);
            body.Add(diagram);

            borderSize = new Label { name = "box-model-border-size", enableRichText = true, parseEscapeSequences = false };
            borderSize.style.fontSize = 12;
            borderSize.style.marginTop = 4;
            borderSize.style.whiteSpace = WhiteSpace.Normal;
            body.Add(borderSize);
            comparison = new InspectorLayoutComparison(HoverLayout);
            body.Add(comparison.Element);
            // 只添加到诊断页提供的内容容器；不占原生属性页的空间。
            inspector.Add(root);
            overlay = new CanvasBoxModelOverlay(viewport);
            // 标签忽略拾取，四个色层接收事件；移到内层或退回外层都按实际目标重新判断。
            diagram.RegisterCallback<PointerOverEvent>(evt => Hover(evt.target as VisualElement));
            diagram.RegisterCallback<PointerMoveEvent>(evt => Hover(evt.target as VisualElement));
            diagram.RegisterCallback<PointerLeaveEvent>(evt =>
            {
                if (hovered != InspectorHighlight.LayoutComparison) ClearHover();
            });
            root.RegisterValueChangedCallback(evt => { if (!evt.newValue) ClearHover(); });
            root.RegisterCallback<DetachFromPanelEvent>(evt => ClearHover());
            RefreshLanguage();
            Refresh(null);
        }

        /// <summary>
        /// 更新静态标签与当前元素的动态说明。语言切换不重建控件，因此折叠、外层滚动和悬停状态都保留。
        /// </summary>
        internal void RefreshLanguage()
        {
            root.text = L10n.Tr("盒模型（计算值）", "Box Model (Computed)");
            borderSize.tooltip = L10n.Tr(
                "悬停盒模型的不同色层，可在画布中查看对应范围。\n" +
                "内容区不含 padding 和 border；外框包含两者，不包含 margin。\n" +
                "数值使用 UI 布局单位，不受画布缩放影响；图形各层不按数值比例绘制。",
                "Hover a box-model layer to inspect its bounds on the canvas.\n" +
                "The content box excludes padding and border; the border box includes both, but excludes margin.\n" +
                "Values use UI layout units and are independent of canvas zoom; diagram layers are not drawn to scale.");
            comparison.RefreshLanguage();
            overlay.RefreshLanguage();
            Refresh(inspectedElement, previewEnabled);
        }

        /// <summary>
        /// 读取已布局的 live 元素。百分比、auto、flex 收缩等都在这里变为最终数值。
        /// contentRect 是公开的内容区接口；layout 是 border-box，不能拿它冒充内容区宽高。
        /// </summary>
        internal void Refresh(VisualElement element, bool previewEnabled = false)
        {
            if (!ReferenceEquals(inspectedElement, element)) ClearHover();
            inspectedElement = element;
            this.previewEnabled = previewEnabled;
            if (previewEnabled) ClearHover();
            comparison.Refresh(element);
            bool available = element != null && element.panel != null;
            diagram.style.display = borderSize.style.display = available ? DisplayStyle.Flex : DisplayStyle.None;
            if (!available)
            {
                ClearHover();
                target.text = L10n.Tr("选择 Hierarchy 或画布中的元素", "Select an element in the Hierarchy or canvas");
                target.tooltip = string.Empty;
                return;
            }

            target.tooltip = StyleSourceReader.Describe(element);
            target.text = DiagnosticText.Identity(target.tooltip, UnityEditor.EditorGUIUtility.isProSkin);
            IResolvedStyle style = element.resolvedStyle;
            margin.Set(style.marginTop, style.marginRight, style.marginBottom, style.marginLeft);
            border.Set(style.borderTopWidth, style.borderRightWidth, style.borderBottomWidth, style.borderLeftWidth);
            padding.Set(style.paddingTop, style.paddingRight, style.paddingBottom, style.paddingLeft);
            contentSize.text = Format(element.contentRect.width) + " × " + Format(element.contentRect.height);
            borderSize.text = DiagnosticText.Colorize(L10n.Tr("外框 ", "Border box "), DiagnosticTone.Property,
                    UnityEditor.EditorGUIUtility.isProSkin) +
                DiagnosticText.Value(Format(element.layout.width) + " × " + Format(element.layout.height) + " px", UnityEditor.EditorGUIUtility.isProSkin);
            if (hovered == InspectorHighlight.LayoutComparison) overlay.ShowLayout(element);
            else if (hovered.HasValue) overlay.Show(element, RegionOf(hovered.Value));
        }

        void Hover(VisualElement hit)
        {
            if (inspectedElement == null || inspectedElement.panel == null || previewEnabled) return;
            hovered = hit == content || content.Contains(hit) ? InspectorHighlight.Content :
                hit == padding.Element || padding.Element.Contains(hit) ? InspectorHighlight.Padding :
                hit == border.Element || border.Element.Contains(hit) ? InspectorHighlight.Border : InspectorHighlight.Margin;
            SetLayerColors();
            overlay.Show(inspectedElement, RegionOf(hovered.Value));
        }

        void HoverLayout(bool active)
        {
            if (!active)
            {
                if (hovered == InspectorHighlight.LayoutComparison) ClearHover();
                return;
            }
            if (inspectedElement == null || inspectedElement.panel == null || inspectedElement.hierarchy.parent == null || previewEnabled) return;
            hovered = InspectorHighlight.LayoutComparison;
            SetLayerColors();
            overlay.ShowLayout(inspectedElement);
        }

        /// <summary>离开盒模型、切换元素、折叠、失焦和释放时都清除临时调试层，保留原生选择。</summary>
        internal void ClearHover()
        {
            // 没有持有右侧悬停时，不清掉 Viewport 正在使用的共享覆盖层。
            if (!hovered.HasValue) return;
            CanvasOverlayOwner owner = hovered == InspectorHighlight.LayoutComparison ?
                CanvasOverlayOwner.LayoutComparison : CanvasOverlayOwner.BoxLayer;
            hovered = null;
            SetLayerColors();
            overlay.Hide(owner);
        }

        void SetLayerColors()
        {
            margin.Element.style.backgroundColor = LayerColor(BoxModelRegion.Margin);
            border.Element.style.backgroundColor = LayerColor(BoxModelRegion.Border);
            padding.Element.style.backgroundColor = LayerColor(BoxModelRegion.Padding);
            content.style.backgroundColor = LayerColor(BoxModelRegion.Content);
        }

        Color LayerColor(BoxModelRegion region)
        {
            Color color = CanvasBoxModelOverlay.ColorFor(region);
            return hovered.HasValue && hovered != InspectorHighlight.LayoutComparison && RegionOf(hovered.Value) != region
                ? Color.Lerp(color, new Color(0.43f, 0.43f, 0.43f), 0.75f) : color;
        }

        static BoxModelRegion RegionOf(InspectorHighlight highlight)
        {
            switch (highlight)
            {
                case InspectorHighlight.Margin: return BoxModelRegion.Margin;
                case InspectorHighlight.Border: return BoxModelRegion.Border;
                case InspectorHighlight.Padding: return BoxModelRegion.Padding;
                default: return BoxModelRegion.Content;
            }
        }

        public void Dispose()
        {
            ClearHover();
            root.RemoveFromHierarchy();
            overlay.Dispose();
        }

        internal static string Format(float value) => float.IsNaN(value) || float.IsInfinity(value)
            ? "—" : value.ToString("0.##", CultureInfo.InvariantCulture);

        /// <summary>各层留出固定示意宽度，保证 0、负 margin 和很大的间距都能读清，不改变真实元素。</summary>
        static VisualElement Inset(VisualElement parent)
        {
            var element = new VisualElement();
            element.style.position = Position.Absolute;
            element.style.left = element.style.right = 32;
            element.style.top = element.style.bottom = 22;
            parent.Add(element);
            return element;
        }

        static Label Caption(string text)
        {
            var label = new Label(text) { pickingMode = PickingMode.Ignore };
            label.style.fontSize = 12;
            label.style.color = (Color)new Color32(32, 35, 38, 255);
            label.style.marginLeft = label.style.marginRight = 0;
            label.style.marginTop = label.style.marginBottom = 0;
            label.style.paddingLeft = label.style.paddingRight = 0;
            label.style.paddingTop = label.style.paddingBottom = 0;
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
            return label;
        }

        /// <summary>单层的四边标注；位置固定为上、右、下、左，值保持 resolvedStyle 的符号与精度。</summary>
        sealed class Edges
        {
            internal VisualElement Element { get; }
            readonly Label top;
            readonly Label right;
            readonly Label bottom;
            readonly Label left;

            internal Edges(VisualElement element, string name, Color color)
            {
                Element = element;
                element.style.backgroundColor = color;
                Label title = Caption(name);
                title.style.position = Position.Absolute;
                title.style.left = 5;
                title.style.top = 2;
                title.style.fontSize = 11;
                element.Add(title);
                top = AddValue(element, name + "-top");
                bottom = AddValue(element, name + "-bottom");
                top.style.top = bottom.style.bottom = 0;
                top.style.left = top.style.right = bottom.style.left = bottom.style.right = Length.Percent(35);
                top.style.height = bottom.style.height = 22;
                left = AddValue(element, name + "-left");
                right = AddValue(element, name + "-right");
                left.style.left = right.style.right = 0;
                left.style.top = left.style.bottom = right.style.top = right.style.bottom = 22;
                left.style.width = right.style.width = 32;
            }

            static Label AddValue(VisualElement parent, string name)
            {
                Label label = Caption(string.Empty);
                label.name = "box-model-" + name;
                label.style.position = Position.Absolute;
                parent.Add(label);
                return label;
            }

            internal void Set(float top, float right, float bottom, float left)
            {
                this.top.text = Format(top);
                this.right.text = Format(right);
                this.bottom.text = Format(bottom);
                this.left.text = Format(left);
            }
        }
    }
}
