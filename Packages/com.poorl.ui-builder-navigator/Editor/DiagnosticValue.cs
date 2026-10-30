using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace UIBuilderUssNavigator
{
    /// <summary>
    /// 右侧诊断面板共用的只读值：语义配色文字和可选颜色块。
    /// 原文保存在 Label.userData，富文本仅用于显示；值和主题都未变化时不重新格式化。
    /// 控件不写样式资源、不接受键盘编辑；普通值忽略拾取，保留布局表的整表悬停行为。
    /// </summary>
    internal sealed class DiagnosticValue : VisualElement
    {
        readonly Func<string, bool, string> format;
        readonly ReadOnlyColorSwatch swatch;
        string lastText;
        bool lastDark;
        int lastLanguageRevision = -1;

        internal Label TextLabel { get; }

        internal DiagnosticValue(string name, Func<string, bool, string> format)
        {
            this.format = format;
            pickingMode = PickingMode.Ignore;
            style.flexDirection = FlexDirection.Row;
            style.alignItems = Align.Center;
            style.minWidth = 0;
            swatch = new ReadOnlyColorSwatch();
            Add(swatch);
            TextLabel = new Label
            {
                name = name, pickingMode = PickingMode.Ignore, enableRichText = true, parseEscapeSequences = false
            };
            TextLabel.style.fontSize = 12;
            TextLabel.style.whiteSpace = WhiteSpace.Normal;
            TextLabel.style.flexShrink = 1;
            TextLabel.style.minWidth = 0;
            TextLabel.style.marginLeft = TextLabel.style.marginRight = 0;
            TextLabel.style.marginTop = TextLabel.style.marginBottom = 0;
            Add(TextLabel);
        }

        internal void SetValue(string text, Color? color = null)
        {
            bool dark = EditorGUIUtility.isProSkin;
            if (lastText != text || lastDark != dark || lastLanguageRevision != L10n.Revision)
            {
                TextLabel.userData = text;
                TextLabel.text = format(text, dark);
                lastText = text;
                lastDark = dark;
                lastLanguageRevision = L10n.Revision;
            }
            swatch.SetColor(color);
        }

        internal void RefreshLanguage()
        {
            lastLanguageRevision = -1;
            SetValue(lastText ?? string.Empty, swatch.Color);
        }
    }

    /// <summary>
    /// 原生绘制的颜色样本。棋盘底显示透明度，实际 Color 覆盖在上层；无 ColorField、弹窗或写入事件。
    /// 仅色块接收拾取以显示 RGBA tooltip，未显示色块的布局表单元格继续忽略拾取。
    /// </summary>
    internal sealed class ReadOnlyColorSwatch : VisualElement
    {
        Color? color;
        int languageRevision = -1;

        internal Color? Color => color;

        internal ReadOnlyColorSwatch()
        {
            name = "diagnostic-color-swatch";
            focusable = false;
            style.width = style.height = 16;
            style.flexShrink = 0;
            style.marginRight = 6;
            style.alignSelf = Align.Center;
            style.borderLeftWidth = style.borderRightWidth = style.borderTopWidth = style.borderBottomWidth = 1;
            style.borderLeftColor = style.borderRightColor = style.borderTopColor = style.borderBottomColor =
                new Color(0.48f, 0.53f, 0.6f);
            style.display = DisplayStyle.None;
            generateVisualContent += Draw;
        }

        internal void SetColor(Color? value)
        {
            if (Nullable.Equals(color, value) && languageRevision == L10n.Revision) return;
            color = value;
            languageRevision = L10n.Revision;
            style.display = value.HasValue ? DisplayStyle.Flex : DisplayStyle.None;
            if (value.HasValue)
            {
                Color rgba = value.Value;
                tooltip = "#" + ColorUtility.ToHtmlStringRGBA(rgba) + "\nRGBA (" +
                    InspectorBoxModel.Format(rgba.r) + ", " + InspectorBoxModel.Format(rgba.g) + ", " +
                    InspectorBoxModel.Format(rgba.b) + ", " + InspectorBoxModel.Format(rgba.a) + ")\n" +
                    L10n.Tr("只读颜色预览", "Read-only color preview");
            }
            else tooltip = string.Empty;
            MarkDirtyRepaint();
        }

        void Draw(MeshGenerationContext context)
        {
            if (!color.HasValue) return;
            Rect bounds = contentRect;
            Painter2D painter = context.painter2D;
            float halfWidth = bounds.width / 2;
            float halfHeight = bounds.height / 2;
            for (int y = 0; y < 2; y++)
                for (int x = 0; x < 2; x++)
                    Fill(painter, new Rect(bounds.x + x * halfWidth, bounds.y + y * halfHeight, halfWidth, halfHeight),
                        (x + y) % 2 == 0 ? (Color)new Color32(204, 210, 218, 255) : (Color)new Color32(118, 126, 138, 255));
            Fill(painter, bounds, color.Value);
        }

        static void Fill(Painter2D painter, Rect rect, Color value)
        {
            painter.fillColor = value;
            painter.BeginPath();
            painter.MoveTo(new Vector2(rect.xMin, rect.yMin));
            painter.LineTo(new Vector2(rect.xMax, rect.yMin));
            painter.LineTo(new Vector2(rect.xMax, rect.yMax));
            painter.LineTo(new Vector2(rect.xMin, rect.yMax));
            painter.ClosePath();
            painter.Fill();
        }
    }
}
