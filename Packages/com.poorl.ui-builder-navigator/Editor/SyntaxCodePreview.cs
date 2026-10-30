using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace UIBuilderUssNavigator
{
    /// <summary>
    /// USS 与 UXML 共用的只读、按行虚拟化代码视图。
    /// 源码与显示标记分开保存；定位蓝底只是临时提示，不是用户的文字选区。
    /// </summary>
    internal sealed class SyntaxCodePreview : IDisposable
    {
        const int RowHeight = 18;
        readonly VisualElement preview;
        readonly ScrollView nativeScroll;
        readonly TextField nativeCode;
        readonly VisualElement view;
        readonly Label status;
        readonly ListView lines;
        readonly PreviewSearchBar search;
        readonly Func<IReadOnlyList<string>, bool, string[]> highlight;
        readonly Func<string> locationCopyLabel;
        readonly List<string> sourceLines = new List<string>();
        string[] coloredLines = Array.Empty<string>();
        string sourceText;
        bool darkSkin;
        bool hasRange;
        bool locationVisible;
        RuleRange range;
        bool pendingScroll;
        bool pendingSearchScroll;
        bool pendingWidth;
        float rowWidth;
        string statusZh;
        string statusEn;

        internal bool IsAttachedTo(VisualElement currentPreview) => ReferenceEquals(preview, currentPreview) &&
            preview.panel != null && view.parent == preview;

        internal SyntaxCodePreview(VisualElement preview, string prefix,
            Func<IReadOnlyList<string>, bool, string[]> highlight, Func<string> locationCopyLabel)
        {
            this.preview = preview;
            this.highlight = highlight;
            this.locationCopyLabel = locationCopyLabel ?? throw new ArgumentNullException(nameof(locationCopyLabel));
            nativeScroll = preview.Q<ScrollView>(className: "unity-builder-code__scroll-view");
            nativeCode = preview.Q<TextField>("unity-builder-code__code");
            if (nativeScroll == null || nativeCode == null)
                throw new NotSupportedException(L10n.Tr("找不到原生代码预览区域：", "Native code preview area was not found: ") + prefix);

            view = new VisualElement { name = prefix + "-view" };
            view.style.position = Position.Absolute;
            view.style.left = view.style.right = view.style.top = view.style.bottom = 0;
            search = new PreviewSearchBar(prefix, SearchChanged);
            view.Add(search.Root);
            view.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (!(evt.ctrlKey || evt.commandKey) || evt.keyCode != KeyCode.F) return;
                search.Focus();
                evt.StopPropagation();
            }, TrickleDown.TrickleDown);
            status = new Label { name = prefix + "-status" };
            status.style.fontSize = 11;
            status.style.paddingLeft = 8;
            status.style.height = 24;
            status.style.flexShrink = 0;
            status.style.unityTextAlign = TextAnchor.MiddleLeft;
            status.style.whiteSpace = WhiteSpace.NoWrap;
            view.Add(status);

            lines = new ListView
            {
                name = prefix + "-lines",
                itemsSource = sourceLines,
                fixedItemHeight = RowHeight,
                virtualizationMethod = CollectionVirtualizationMethod.FixedHeight,
                selectionType = SelectionType.None,
                horizontalScrollingEnabled = true,
                makeItem = MakeRow,
                bindItem = BindRow
            };
            lines.style.flexGrow = 1;
            lines.style.minHeight = 0;
            lines.AddManipulator(new ContextualMenuManipulator(menu =>
            {
                // 菜单在打开时读取当前语言，切换语言无需重建 ListView 或其虚拟行。
                menu.menu.AppendAction(this.locationCopyLabel(), _ => CopyRule(),
                    _ => hasRange ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
                menu.menu.AppendAction(L10n.Tr("复制完整预览", "Copy full preview"),
                    _ => EditorGUIUtility.systemCopyBuffer = sourceText);
            }));
            view.Add(lines);
            preview.Add(view);
            nativeScroll.style.display = DisplayStyle.None;
        }

        /// <summary>仅在文本或主题变化时重新配色；点击取消蓝底后，普通轮询不会把它重新点亮。</summary>
        internal bool SetText(string text)
        {
            bool textChanged = sourceText != text;
            bool themeChanged = darkSkin != EditorGUIUtility.isProSkin;
            if (!textChanged && !themeChanged) return false;
            sourceText = text;
            if (textChanged)
            {
                sourceLines.Clear();
                sourceLines.AddRange(text.Replace("\r\n", "\n").Split('\n'));
            }
            darkSkin = EditorGUIUtility.isProSkin;
            coloredLines = highlight(sourceLines, darkSkin);
            if (textChanged) search.SetSource(sourceLines);
            pendingWidth = true;
            lines.RefreshItems();
            SetStatus("完整预览 · " + sourceLines.Count + " 行", "Full preview · " + sourceLines.Count + " lines");
            return true;
        }

        /// <summary>同时保存两种语言的状态源文案，切换时不重新定位、滚动或绑定虚拟行。</summary>
        internal void SetStatus(string zh, string en)
        {
            statusZh = zh;
            statusEn = en;
            ApplyStatusLanguage();
        }

        internal void RefreshLanguage()
        {
            search.RefreshLanguage();
            ApplyStatusLanguage();
        }

        void ApplyStatusLanguage()
        {
            string text = L10n.Tr(statusZh ?? string.Empty, statusEn ?? string.Empty);
            status.text = text;
            status.tooltip = text;
        }

        /// <summary>保留规则范围供复制；是否显示和滚动到范围，由明确的定位动作决定。</summary>
        internal void SetLocation(bool found, RuleRange location, bool reveal)
        {
            hasRange = found;
            range = location;
            if (!found) ClearLocation();
            else if (reveal)
            {
                locationVisible = true;
                pendingScroll = true;
                pendingSearchScroll = false;
            }
            UpdateRowBackgrounds();
        }

        /// <summary>
        /// 鼠标点击只撤销自动定位背景，不重绑行、不更改原生文字选择，也不拦截事件。
        /// 否则重新创建/绑定行会干扰同一次按下事件启动的手动拖选。
        /// </summary>
        internal void ClearLocation()
        {
            locationVisible = false;
            pendingScroll = false;
            UpdateRowBackgrounds();
        }

        void UpdateRowBackgrounds()
        {
            foreach (Label code in lines.Query<Label>("line-code").ToList())
                SetRowBackground(code.parent, (int)code.parent.userData);
        }

        void SetRowBackground(VisualElement row, int index)
        {
            // 虚拟行会复用；显式透明色可避免上一条规则的蓝底残留。
            row.style.backgroundColor = hasRange && locationVisible && index >= range.StartLine && index <= range.EndLine
                ? new Color(0.22f, 0.48f, 0.85f, 0.28f) : Color.clear;
        }

        VisualElement MakeRow()
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.height = RowHeight;
            row.style.flexShrink = 0;
            var number = new Label { name = "line-number", pickingMode = PickingMode.Ignore };
            number.style.width = 48;
            number.style.flexShrink = 0;
            number.style.paddingRight = 10;
            number.style.unityTextAlign = TextAnchor.UpperRight;
            number.style.color = new Color(0.55f, 0.55f, 0.55f);
            row.Add(number);
            var code = new Label { name = "line-code", focusable = true };
            code.enableRichText = true;
            code.parseEscapeSequences = false;
            code.selection.isSelectable = true;
            code.selection.selectAllOnFocus = false;
            code.selection.selectAllOnMouseUp = false;
            row.Add(code);
            return row;
        }

        void BindRow(VisualElement row, int index)
        {
            Label number = row.Q<Label>("line-number");
            Label code = row.Q<Label>("line-code");
            string displayed = search.MarkLine(coloredLines[index], index, darkSkin);
            // 只有行被用于另一段源码时才清除手动选区；定位背景的取消不走此路径。
            if (!(row.userData is int previous) || previous != index || code.text != displayed)
                code.selection.SelectNone();
            row.userData = index;
            row.style.minWidth = rowWidth;
            SetRowBackground(row, index);
            number.text = (index + 1).ToString();
            code.text = displayed;
            foreach (Label label in new[] { number, code })
            {
                label.style.marginTop = label.style.marginBottom = 0;
                label.style.paddingTop = label.style.paddingBottom = 0;
                label.style.fontSize = nativeCode.resolvedStyle.fontSize;
                label.style.unityFontDefinition = nativeCode.resolvedStyle.unityFontDefinition;
                label.style.whiteSpace = WhiteSpace.NoWrap;
            }
            code.style.color = nativeCode.resolvedStyle.color;
        }

        /// <summary>沿用 Editor update 消费一次布局/定位请求；窗口缩放不强制重置用户浏览位置。</summary>
        internal void UpdateLayout()
        {
            if (!pendingWidth && !pendingScroll && !pendingSearchScroll) return;
            if (lines.panel == null || lines.resolvedStyle.height <= 0) return;
            if (pendingWidth)
            {
                Label sample = lines.Q<Label>("line-code");
                if (sample == null) return;
                float width = 0;
                foreach (string line in coloredLines)
                {
                    float measured = sample.MeasureTextSize(line, 0, VisualElement.MeasureMode.Undefined,
                        0, VisualElement.MeasureMode.Undefined).x;
                    if (float.IsNaN(measured)) return;
                    width = Mathf.Max(width, measured);
                }
                rowWidth = Mathf.Ceil(width) + 64;
                lines.Q<ScrollView>().contentContainer.style.minWidth = rowWidth;
                pendingWidth = false;
                lines.RefreshItems();
                // 修改宽度会重建虚拟行；等这一轮布局完成后再导航。
                return;
            }
            if (pendingSearchScroll)
            {
                RevealSearchMatch();
                return;
            }
            if (!pendingScroll) return;
            ScrollView scroll = lines.Q<ScrollView>();
            float horizontalOffset = scroll.scrollOffset.x;
            int visibleRows = Mathf.Max(1, (int)(scroll.contentViewport.resolvedStyle.height / RowHeight));
            lines.ScrollToItem(range.StartLine);
            lines.ScrollToItem(Mathf.Min(range.EndLine, range.StartLine + Mathf.Max(0, visibleRows - 2)));
            scroll.horizontalScroller.value = horizontalOffset;
            pendingScroll = false;
        }

        /// <summary>显式搜索导航拥有自己的滚动请求，不复用/替换元素与 USS 规则的定位范围。</summary>
        void SearchChanged(bool reveal)
        {
            pendingSearchScroll = reveal;
            if (reveal) pendingScroll = false;
            lines.RefreshItems();
        }

        void RevealSearchMatch()
        {
            SearchMatch? active = search.ActiveMatch;
            if (!active.HasValue)
            {
                pendingSearchScroll = false;
                return;
            }
            Label sample = lines.Q<Label>("line-code");
            if (sample == null) return;
            SearchMatch match = active.Value;
            string source = sourceLines[match.Line];
            float left = MeasureSource(sample, source.Substring(0, match.Column)) + 48;
            float right = MeasureSource(sample, source.Substring(0, match.Column + match.Length)) + 48;
            if (float.IsNaN(left) || float.IsNaN(right)) return;
            lines.ScrollToItem(match.Line);
            ScrollView scroll = lines.Q<ScrollView>();
            float offset = scroll.scrollOffset.x;
            float visibleWidth = scroll.contentViewport.resolvedStyle.width;
            if (left < offset) scroll.horizontalScroller.value = Mathf.Max(0, left - 12);
            else if (right > offset + visibleWidth)
                scroll.horizontalScroller.value = Mathf.Max(0, right - visibleWidth + 12);
            pendingSearchScroll = false;
        }

        static float MeasureSource(Label sample, string text)
        {
            // 源码中的 <color> 等也按字面量测宽，防止水平跳转把源码误当作富文本标签。
            var literal = new System.Text.StringBuilder();
            SyntaxText.AppendLiteral(literal, text);
            return sample.MeasureTextSize(literal.ToString(), 0, VisualElement.MeasureMode.Undefined,
                0, VisualElement.MeasureMode.Undefined).x;
        }

        void CopyRule() => EditorGUIUtility.systemCopyBuffer = string.Join("\n",
            sourceLines.Skip(range.StartLine).Take(range.EndLine - range.StartLine + 1));

        public void Dispose()
        {
            view.RemoveFromHierarchy();
            // 本版原生代码区撤销为 Null 后仍可能不显示，显式恢复正常的 Flex 布局。
            nativeScroll.style.display = DisplayStyle.Flex;
        }
    }
}
