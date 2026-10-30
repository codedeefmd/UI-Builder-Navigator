using System;
using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UIBuilderUssNavigator
{
    /// <summary>
    /// 单个代码预览的搜索栏和命中状态；UXML/USS 各持有一份，查询与跳转互不影响。
    /// 搜索完整内存文本，大小写不敏感、逐行文字匹配；不筛掉源码行，也不改写文档或原生选择。
    /// </summary>
    internal sealed class PreviewSearchBar
    {
        readonly ToolbarSearchField field;
        readonly TextField input;
        readonly Label counter;
        readonly Button previous;
        readonly Button next;
        readonly Action<bool> changed;
        readonly Dictionary<int, List<SearchMatch>> byLine = new Dictionary<int, List<SearchMatch>>();
        IReadOnlyList<string> source = Array.Empty<string>();
        List<SearchMatch> matches = new List<SearchMatch>();
        int activeIndex = -1;

        internal VisualElement Root { get; }
        internal SearchMatch? ActiveMatch => activeIndex < 0 ? (SearchMatch?)null : matches[activeIndex];

        internal PreviewSearchBar(string prefix, Action<bool> changed)
        {
            this.changed = changed;
            Root = new VisualElement { name = prefix + "-find-bar" };
            Root.style.flexDirection = FlexDirection.Row;
            Root.style.alignItems = Align.Center;
            Root.style.height = 24;
            Root.style.flexShrink = 0;
            Root.style.paddingLeft = Root.style.paddingRight = 4;
            field = new ToolbarSearchField
            {
                name = prefix + "-find"
            };
            field.style.flexGrow = 1;
            field.style.minWidth = 30;
            input = field.Q<TextField>();
            counter = new Label { name = prefix + "-find-count" };
            counter.style.fontSize = 11;
            counter.style.minWidth = 42;
            counter.style.unityTextAlign = TextAnchor.MiddleCenter;
            previous = NavigationButton("↑", -1, prefix + "-find-previous");
            next = NavigationButton("↓", 1, prefix + "-find-next");
            Root.Add(field);
            Root.Add(counter);
            Root.Add(previous);
            Root.Add(next);
            field.RegisterValueChangedCallback(evt =>
            {
                Rebuild(false);
                this.changed(ActiveMatch.HasValue);
            });
            // 在输入框处理回车前消费导航键，不把 Enter 留给文本框或按钮重复处理。
            Root.RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
            RefreshLanguage();
        }

        Button NavigationButton(string text, int direction, string name)
        {
            var button = new Button(() => Move(direction)) { text = text, name = name, focusable = false };
            button.style.width = button.style.minWidth = 22;
            button.style.height = 18;
            button.style.marginLeft = button.style.marginRight = 1;
            button.style.marginTop = button.style.marginBottom = 0;
            button.style.paddingLeft = button.style.paddingRight = 0;
            return button;
        }

        /// <summary>源码更新时优先保留同一行列的命中；没有它时保留尽量接近的结果序号，不强制滚动。</summary>
        internal void SetSource(IReadOnlyList<string> lines)
        {
            source = lines;
            Rebuild(true);
        }

        void Rebuild(bool preserve)
        {
            SearchMatch? oldMatch = ActiveMatch;
            int oldIndex = activeIndex;
            matches = CodePreviewSearch.FindMatches(source, field.value);
            activeIndex = matches.Count == 0 ? -1 : 0;
            if (preserve && oldMatch.HasValue && matches.Count > 0)
            {
                int same = matches.FindIndex(match => match.Line == oldMatch.Value.Line && match.Column == oldMatch.Value.Column);
                activeIndex = same >= 0 ? same : Math.Min(oldIndex, matches.Count - 1);
            }
            byLine.Clear();
            foreach (SearchMatch match in matches)
            {
                if (!byLine.TryGetValue(match.Line, out List<SearchMatch> row))
                    byLine.Add(match.Line, row = new List<SearchMatch>());
                row.Add(match);
            }
            UpdateCounter();
        }

        void Move(int direction)
        {
            if (matches.Count == 0) return;
            activeIndex = (activeIndex + direction + matches.Count) % matches.Count;
            UpdateCounter();
            changed(true);
        }

        void UpdateCounter()
        {
            counter.text = (activeIndex + 1) + "/" + matches.Count;
            counter.tooltip = matches.Count == 0 && !string.IsNullOrEmpty(field.value)
                ? L10n.Tr("未找到匹配项", "No matches")
                : L10n.Tr("当前结果 / 全部结果；到末尾后循环查找",
                    "Current result / total; search wraps at the end");
            previous.SetEnabled(matches.Count > 0);
            next.SetEnabled(matches.Count > 0);
        }

        /// <summary>
        /// 语言切换只替换搜索栏的静态提示和计数说明。查询字符串、命中集合、当前序号、
        /// 输入焦点与外层 ListView 的滚动都继续沿用，避免翻译动作变成一次新的搜索操作。
        /// </summary>
        internal void RefreshLanguage()
        {
            field.placeholderText = L10n.Tr("查找当前预览…", "Find in current preview…");
            field.tooltip = L10n.Tr(
                "查找当前完整预览（忽略大小写）；Ctrl+F 聚焦，Enter / Shift+Enter 切换，Esc 清空",
                "Search the full current preview (case-insensitive); Ctrl+F focuses, Enter / Shift+Enter navigates, Esc clears");
            previous.tooltip = L10n.Tr("上一个结果（Shift+Enter）", "Previous result (Shift+Enter)");
            next.tooltip = L10n.Tr("下一个结果（Enter）", "Next result (Enter)");
            UpdateCounter();
        }

        void OnKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
                Move(evt.shiftKey ? -1 : 1);
            else if (evt.keyCode == KeyCode.Escape)
            {
                field.value = string.Empty;
                input.Blur();
            }
            else return;
            evt.StopPropagation();
        }

        internal void Focus()
        {
            input.Focus();
            input.SelectAll();
        }

        /// <summary>命中只叠加显示标记，语法配色与原文保持；没有命中的行直接返回原配色字符串。</summary>
        internal string MarkLine(string colored, int line, bool darkSkin)
        {
            if (!byLine.TryGetValue(line, out List<SearchMatch> row)) return colored;
            SearchMatch? active = ActiveMatch;
            int column = active.HasValue && active.Value.Line == line ? active.Value.Column : -1;
            return CodePreviewSearch.MarkLine(colored, row, column, darkSkin);
        }
    }
}
