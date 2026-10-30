using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace UIBuilderUssNavigator
{
    internal enum BoxModelRegion { Margin, Border, Padding, Content }
    internal enum CanvasOverlayOwner { None, BoxLayer, ViewportInspection, LayoutComparison }

    /// <summary>
    /// 盒模型悬停时的画布调试层。挂在编辑器 viewport，不进入用户文档，不参与拾取或页面布局。
    /// 四层矩形以被检查元素的局部坐标计算，再逐角转换到覆盖层，跟随画布缩放、平移和元素变换。
    /// 与 Inspector 共用刷新周期；隐藏、关闭插件和脚本重载时释放自身控件。
    /// </summary>
    internal sealed class CanvasBoxModelOverlay : IDisposable
    {
        // 调试文字使用低饱和度语义色；属性、数值、单位分层，避免整块纯白小字挤在一起。
        const string PropertyColor = "A7B8CC";
        const string ValueColor = "BED9CA";
        const string MutedColor = "96A5B8";
        const string TypeColor = "B2CCEF";
        const string NameColor = "D7BFA4";
        const string ClassColor = "C6B6D9";
        readonly VisualElement viewport;
        readonly VisualElement root;
        readonly VisualElement info;
        readonly Label identity;
        readonly Label metrics;
        VisualElement element;
        BoxModelRegion region;
        CanvasOverlayOwner owner;
        VisualElement parentElement;
        Rect parentBox;
        Rect marginBox;
        Rect borderBox;
        Rect paddingBox;
        Rect contentBox;

        internal bool IsAttached => root.parent == viewport && root.panel != null;
        internal bool IsInspection => owner == CanvasOverlayOwner.ViewportInspection && element != null;
        internal static Color ParentLayoutColor => new Color32(179, 156, 217, 255);
        internal static Color CurrentLayoutColor => new Color32(127, 184, 226, 255);

        internal CanvasBoxModelOverlay(VisualElement viewport)
        {
            this.viewport = viewport;
            root = new VisualElement { name = "box-model-canvas-overlay", pickingMode = PickingMode.Ignore };
            root.StretchToParentSize();
            root.style.overflow = Overflow.Hidden;
            root.style.display = DisplayStyle.None;
            root.generateVisualContent += Draw;
            info = new VisualElement { name = "box-model-canvas-info", pickingMode = PickingMode.Ignore };
            info.style.position = Position.Absolute;
            info.style.backgroundColor = (Color)new Color32(34, 38, 46, 255);
            info.style.paddingLeft = info.style.paddingRight = 10;
            info.style.paddingTop = info.style.paddingBottom = 7;
            info.style.borderLeftWidth = 3;
            identity = InfoLabel("box-model-canvas-identity");
            identity.style.unityFontStyleAndWeight = FontStyle.Bold;
            identity.style.fontSize = 13;
            identity.style.overflow = Overflow.Hidden;
            identity.style.textOverflow = TextOverflow.Ellipsis;
            metrics = InfoLabel("box-model-canvas-metrics");
            metrics.style.whiteSpace = WhiteSpace.Normal;
            metrics.style.marginTop = 3;
            info.Add(identity);
            info.Add(metrics);
            root.Add(info);
            // 自有覆盖层是 viewport-surface 的兄弟项，原生 canvas/highlight/pick overlay 均保持原样。
            viewport.Add(root);
        }

        internal void Show(VisualElement target, BoxModelRegion selectedRegion)
        {
            owner = CanvasOverlayOwner.BoxLayer;
            element = target;
            region = selectedRegion;
            Refresh();
        }

        /// <summary>画布悬停模式同时展示四层与元素概要；与右侧盒模型逐层检查复用同一个只读覆盖层。</summary>
        internal void ShowInspection(VisualElement target)
        {
            owner = CanvasOverlayOwner.ViewportInspection;
            element = target;
            Refresh();
        }

        /// <summary>父内容区与当前 border-box 同时显示，各自按局部坐标逐角变换，不使用 worldBound 代替轮廓。</summary>
        internal void ShowLayout(VisualElement target)
        {
            owner = CanvasOverlayOwner.LayoutComparison;
            element = target;
            Refresh();
        }

        internal void Hide(CanvasOverlayOwner expectedOwner)
        {
            if (owner == expectedOwner) Hide();
        }

        internal void Hide()
        {
            element = null;
            parentElement = null;
            owner = CanvasOverlayOwner.None;
            root.style.display = DisplayStyle.None;
        }

        /// <summary>语言切换时重绘仍在显示的信息条；几何、覆盖层所有权和画布选择均保持。</summary>
        internal void RefreshLanguage()
        {
            if (element != null) Refresh();
        }

        internal void Refresh()
        {
            if (element == null || element.panel != root.panel || element.resolvedStyle.display == DisplayStyle.None ||
                !IsFinite(element.layout.width) || !IsFinite(element.layout.height))
            {
                Hide();
                return;
            }
            var style = element.resolvedStyle;
            bool inspection = IsInspection;
            borderBox = new Rect(0, 0, element.layout.width, element.layout.height);
            paddingBox = Rect.MinMaxRect(style.borderLeftWidth, style.borderTopWidth,
                borderBox.width - style.borderRightWidth, borderBox.height - style.borderBottomWidth);
            contentBox = element.contentRect;
            marginBox = Rect.MinMaxRect(-style.marginLeft, -style.marginTop,
                borderBox.width + style.marginRight, borderBox.height + style.marginBottom);
            root.style.display = DisplayStyle.Flex;
            if (owner == CanvasOverlayOwner.LayoutComparison)
            {
                // parent 是逻辑容器；布局按物理层级，例如 ScrollView.contentContainer。
                parentElement = element.hierarchy.parent;
                if (parentElement == null || parentElement.panel != root.panel || parentElement.resolvedStyle.display == DisplayStyle.None ||
                    !IsFinite(parentElement.contentRect.width) || !IsFinite(parentElement.contentRect.height))
                {
                    Hide();
                    return;
                }
                parentBox = parentElement.contentRect;
                info.style.borderLeftColor = ParentLayoutColor;
                identity.text = Colored(L10n.Tr("父子布局对照", "Parent–child layout comparison"), TypeColor);
                metrics.text = Colored(L10n.Tr("父内容区 · ", "Parent content box · "), "B39CD9") +
                    Colored(StyleSourceReader.Describe(parentElement), PropertyColor) +
                    "\n" + Size(parentBox) + Colored(" px", MutedColor) +
                    "\n" + Colored(L10n.Tr("当前外框 · ", "Current border box · "), "7FB8E2") +
                    Colored(StyleSourceReader.Describe(element), PropertyColor) +
                    "\n" + Size(borderBox) + Colored(" px", MutedColor);
                PositionInfo();
                root.MarkDirtyRepaint();
                return;
            }
            parentElement = null;
            info.style.borderLeftColor = ColorFor(inspection ? BoxModelRegion.Content : region);
            identity.text = Colored(element.GetType().Name, TypeColor) +
                (string.IsNullOrEmpty(element.name) ? "" : Colored("#" + element.name, NameColor)) +
                Colored(" · ", MutedColor) + Size(borderBox);
            identity.tooltip = string.Empty;
            if (inspection)
            {
                metrics.text = Colored("class: ", PropertyColor) +
                    string.Join(" ", element.GetClasses().Select(name => Colored("." + name, ClassColor))) +
                    "\n" + Colored("content: ", LayerTextColor(BoxModelRegion.Content)) + Size(contentBox) +
                    Colored(" px  ·  ", MutedColor) + Colored("font-size: ", PropertyColor) +
                    Number(style.fontSize) + Colored(" px", MutedColor) +
                    "\n" + Colored("margin: ", LayerTextColor(BoxModelRegion.Margin)) + Edges(style.marginTop, style.marginRight, style.marginBottom, style.marginLeft) +
                    "\n" + Colored("border: ", LayerTextColor(BoxModelRegion.Border)) + Edges(style.borderTopWidth, style.borderRightWidth, style.borderBottomWidth, style.borderLeftWidth) +
                    "\n" + Colored("padding: ", LayerTextColor(BoxModelRegion.Padding)) + Edges(style.paddingTop, style.paddingRight, style.paddingBottom, style.paddingLeft);
            }
            else
            {
                string values;
                switch (region)
                {
                    case BoxModelRegion.Margin:
                        values = Edges(style.marginTop, style.marginRight, style.marginBottom, style.marginLeft); break;
                    case BoxModelRegion.Border:
                        values = Edges(style.borderTopWidth, style.borderRightWidth, style.borderBottomWidth, style.borderLeftWidth); break;
                    case BoxModelRegion.Padding:
                        values = Edges(style.paddingTop, style.paddingRight, style.paddingBottom, style.paddingLeft); break;
                    default: values = Size(contentBox); break;
                }
                metrics.text = Colored(region.ToString().ToLowerInvariant(), LayerTextColor(region)) +
                    Colored(" · ", MutedColor) + values + Colored("  px", MutedColor);
            }
            PositionInfo();
            root.MarkDirtyRepaint();
        }

        void PositionInfo()
        {
            // 信息条使用画布内的固定字号；定位取变换后的包围盒，靠近边缘时挪到可见区域。
            Rect anchor = root.WorldToLocal(element.worldBound);
            float width = Mathf.Max(0, Mathf.Min(360, viewport.layout.width - 12));
            info.style.width = width;
            float height = info.layout.height > 0 ? info.layout.height : 48;
            info.style.left = Mathf.Clamp(anchor.xMin, 6, Mathf.Max(6, viewport.layout.width - width - 6));
            float top = anchor.yMin - height - 6;
            if (top < 6) top = anchor.yMax + 6;
            info.style.top = Mathf.Clamp(top, 6, Mathf.Max(6, viewport.layout.height - height - 6));
        }

        void Draw(MeshGenerationContext context)
        {
            if (element == null || element.panel != root.panel) return;
            Painter2D painter = context.painter2D;
            if (owner == CanvasOverlayOwner.LayoutComparison)
            {
                CompareBounds(painter, parentElement, parentBox, ParentLayoutColor, 0.1f);
                CompareBounds(painter, element, borderBox, CurrentLayoutColor, 0.18f);
                return;
            }
            // 先显示完整盒模型，再强调悬停层；零宽边不强行画成有宽度的色带。
            Fill(painter, contentBox, BoxModelRegion.Content);
            Ring(painter, paddingBox, contentBox, BoxModelRegion.Padding);
            Ring(painter, borderBox, paddingBox, BoxModelRegion.Border);
            Ring(painter, marginBox, borderBox, BoxModelRegion.Margin);
        }

        /// <summary>四条色带分别绘制，避免填充规则把内层也盖住；负 margin 保留向内的带状范围。</summary>
        void Ring(Painter2D painter, Rect outer, Rect inner, BoxModelRegion layer)
        {
            Fill(painter, Rect.MinMaxRect(outer.xMin, outer.yMin, outer.xMax, inner.yMin), layer);
            Fill(painter, Rect.MinMaxRect(outer.xMin, inner.yMax, outer.xMax, outer.yMax), layer);
            Fill(painter, Rect.MinMaxRect(outer.xMin, inner.yMin, inner.xMin, inner.yMax), layer);
            Fill(painter, Rect.MinMaxRect(inner.xMax, inner.yMin, outer.xMax, inner.yMax), layer);
        }

        void Fill(Painter2D painter, Rect rect, BoxModelRegion layer)
        {
            if (Mathf.Approximately(rect.width, 0) || Mathf.Approximately(rect.height, 0)) return;
            Color color = ColorFor(layer);
            color.a = IsInspection ? 0.28f : region == layer ? 0.5f : 0.12f;
            painter.fillColor = color;
            painter.BeginPath();
            painter.MoveTo(ToOverlay(new Vector2(rect.xMin, rect.yMin)));
            painter.LineTo(ToOverlay(new Vector2(rect.xMax, rect.yMin)));
            painter.LineTo(ToOverlay(new Vector2(rect.xMax, rect.yMax)));
            painter.LineTo(ToOverlay(new Vector2(rect.xMin, rect.yMax)));
            painter.ClosePath();
            painter.Fill();
        }

        Vector2 ToOverlay(Vector2 point) => root.WorldToLocal(element.LocalToWorld(point));

        void CompareBounds(Painter2D painter, VisualElement target, Rect bounds, Color color, float alpha)
        {
            painter.BeginPath();
            painter.MoveTo(LayoutPoint(target, new Vector2(bounds.xMin, bounds.yMin)));
            painter.LineTo(LayoutPoint(target, new Vector2(bounds.xMax, bounds.yMin)));
            painter.LineTo(LayoutPoint(target, new Vector2(bounds.xMax, bounds.yMax)));
            painter.LineTo(LayoutPoint(target, new Vector2(bounds.xMin, bounds.yMax)));
            painter.ClosePath();
            painter.strokeColor = color;
            painter.lineWidth = 1.5f;
            color.a = alpha;
            painter.fillColor = color;
            painter.Fill();
            painter.Stroke();
        }

        Vector2 LayoutPoint(VisualElement target, Vector2 point) => root.WorldToLocal(target.LocalToWorld(point));

        internal static Color ColorFor(BoxModelRegion layer)
        {
            switch (layer)
            {
                case BoxModelRegion.Margin: return new Color32(185, 138, 87, 255);
                case BoxModelRegion.Border: return new Color32(218, 195, 132, 255);
                case BoxModelRegion.Padding: return new Color32(168, 186, 128, 255);
                default: return new Color32(136, 175, 190, 255);
            }
        }

        static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        static string Size(Rect rect) => Number(rect.width) + Colored(" × ", MutedColor) + Number(rect.height);
        static string Edges(float top, float right, float bottom, float left) =>
            Colored(L10n.Tr("上 ", "top "), MutedColor) + Number(top) +
            Colored(L10n.Tr("  右 ", "  right "), MutedColor) + Number(right) +
            Colored(L10n.Tr("  下 ", "  bottom "), MutedColor) + Number(bottom) +
            Colored(L10n.Tr("  左 ", "  left "), MutedColor) + Number(left);

        static string Number(float value) => Colored(InspectorBoxModel.Format(value), ValueColor);

        static string LayerTextColor(BoxModelRegion layer)
        {
            switch (layer)
            {
                case BoxModelRegion.Margin: return "D4B089";
                case BoxModelRegion.Border: return "D8C58F";
                case BoxModelRegion.Padding: return "B3C89F";
                default: return "9FC7D5";
            }
        }

        /// <summary>颜色标签只由插件生成；元素名和 class 等动态内容继续按字面显示，避免被误解析。</summary>
        static string Colored(string text, string color)
        {
            var result = new System.Text.StringBuilder(text.Length + 48);
            result.Append("<color=#").Append(color).Append('>');
            SyntaxText.AppendLiteral(result, text);
            return result.Append("</color>").ToString();
        }

        static Label InfoLabel(string name)
        {
            var label = new Label { name = name, pickingMode = PickingMode.Ignore, enableRichText = true, parseEscapeSequences = false };
            label.style.fontSize = 12;
            label.style.color = (Color)new Color32(203, 211, 222, 255);
            label.style.marginLeft = label.style.marginRight = label.style.marginTop = label.style.marginBottom = 0;
            return label;
        }

        public void Dispose()
        {
            root.generateVisualContent -= Draw;
            root.RemoveFromHierarchy();
            element = null;
            parentElement = null;
            owner = CanvasOverlayOwner.None;
        }
    }
}
