using System;
using System.Collections.Generic;
using System.Text;

namespace UIBuilderUssNavigator
{
    /// <summary>
    /// 将 UXML 按行转换为 TextCore 富文本。扫描器只识别预览所需的 XML 基本词法，不解析或改写文档结构。
    /// 注释、标签和属性字符串的状态跨行保留；输出标签在每行内独立闭合，适用于虚拟化源码列表。
    /// </summary>
    internal static class UxmlSyntaxHighlighter
    {
        readonly struct Palette
        {
            internal readonly string Tag;
            internal readonly string Attribute;
            internal readonly string String;
            internal readonly string Comment;
            internal readonly string Punctuation;
            internal readonly string Text;

            internal Palette(string tag, string attribute, string value, string comment,
                string punctuation, string text)
            {
                Tag = tag;
                Attribute = attribute;
                String = value;
                Comment = comment;
                Punctuation = punctuation;
                Text = text;
            }
        }

        readonly struct Span
        {
            internal readonly string Text;
            internal readonly string Color;

            internal Span(string text, string color)
            {
                Text = text;
                Color = color;
            }
        }

        sealed class ScanState
        {
            internal bool InComment;
            internal bool InTag;
            internal bool ExpectTagName;
            internal char Quote;
        }

        static readonly Palette Dark = new Palette(
            "4EC9B0", "9CDCFE", "CE9178", "6A9955", "808080", "D4D4D4");

        static readonly Palette Light = new Palette(
            "267F99", "0451A5", "A31515", "008000", "505050", "333333");

        /// <summary>
        /// 返回与输入数量相同的高亮行。词法状态只属于本次调用，预览不同 UXML 时不会互相污染。
        /// </summary>
        internal static string[] HighlightLines(IReadOnlyList<string> sourceLines, bool darkSkin)
        {
            if (sourceLines == null) throw new ArgumentNullException(nameof(sourceLines));

            Palette palette = darkSkin ? Dark : Light;
            var state = new ScanState();
            var result = new string[sourceLines.Count];
            for (int index = 0; index < sourceLines.Count; index++)
                result[index] = HighlightLine(sourceLines[index] ?? string.Empty, state, palette);
            return result;
        }

        static string HighlightLine(string line, ScanState state, Palette palette)
        {
            var spans = new List<Span>();
            int index = 0;
            while (index < line.Length)
            {
                if (state.InComment)
                {
                    int end = line.IndexOf("-->", index, StringComparison.Ordinal);
                    if (end < 0)
                    {
                        Add(spans, line.Substring(index), palette.Comment);
                        break;
                    }

                    Add(spans, line.Substring(index, end + 3 - index), palette.Comment);
                    state.InComment = false;
                    index = end + 3;
                    continue;
                }

                if (state.Quote != '\0')
                {
                    int end = line.IndexOf(state.Quote, index);
                    if (end < 0)
                    {
                        Add(spans, line.Substring(index), palette.String);
                        break;
                    }

                    Add(spans, line.Substring(index, end + 1 - index), palette.String);
                    state.Quote = '\0';
                    index = end + 1;
                    continue;
                }

                if (!state.InTag)
                {
                    if (StartsWith(line, index, "<!--"))
                    {
                        int end = line.IndexOf("-->", index + 4, StringComparison.Ordinal);
                        if (end < 0)
                        {
                            Add(spans, line.Substring(index), palette.Comment);
                            state.InComment = true;
                            break;
                        }

                        Add(spans, line.Substring(index, end + 3 - index), palette.Comment);
                        index = end + 3;
                        continue;
                    }

                    if (line[index] == '<')
                    {
                        Add(spans, "<", palette.Punctuation);
                        state.InTag = true;
                        state.ExpectTagName = true;
                        index++;
                        continue;
                    }

                    int textEnd = line.IndexOf('<', index);
                    if (textEnd < 0) textEnd = line.Length;
                    Add(spans, line.Substring(index, textEnd - index), palette.Text);
                    index = textEnd;
                    continue;
                }

                char current = line[index];
                if (current == '\'' || current == '"')
                {
                    state.Quote = current;
                    int end = line.IndexOf(current, index + 1);
                    if (end < 0)
                    {
                        Add(spans, line.Substring(index), palette.String);
                        break;
                    }

                    Add(spans, line.Substring(index, end + 1 - index), palette.String);
                    state.Quote = '\0';
                    index = end + 1;
                    continue;
                }

                if (char.IsWhiteSpace(current))
                {
                    int end = index + 1;
                    while (end < line.Length && char.IsWhiteSpace(line[end])) end++;
                    Add(spans, line.Substring(index, end - index), null);
                    index = end;
                    continue;
                }

                if (current == '>')
                {
                    Add(spans, ">", palette.Punctuation);
                    state.InTag = false;
                    state.ExpectTagName = false;
                    index++;
                    continue;
                }

                if (IsPunctuation(current))
                {
                    Add(spans, current.ToString(), palette.Punctuation);
                    index++;
                    continue;
                }

                if (IsNameCharacter(current))
                {
                    int end = index + 1;
                    while (end < line.Length && IsNameCharacter(line[end])) end++;
                    Add(spans, line.Substring(index, end - index),
                        state.ExpectTagName ? palette.Tag : palette.Attribute);
                    state.ExpectTagName = false;
                    index = end;
                    continue;
                }

                Add(spans, current.ToString(), palette.Punctuation);
                index++;
            }

            return Render(spans);
        }

        static void Add(List<Span> spans, string text, string color)
        {
            if (text.Length == 0) return;
            if (spans.Count > 0 && spans[spans.Count - 1].Color == color)
            {
                Span previous = spans[spans.Count - 1];
                spans[spans.Count - 1] = new Span(previous.Text + text, color);
                return;
            }
            spans.Add(new Span(text, color));
        }

        static string Render(List<Span> spans)
        {
            var result = new StringBuilder();
            foreach (Span span in spans)
            {
                if (span.Color != null) result.Append("<color=#").Append(span.Color).Append('>');
                SyntaxText.AppendLiteral(result, span.Text);
                if (span.Color != null) result.Append("</color>");
            }
            return result.ToString();
        }

        static bool StartsWith(string text, int index, string value) =>
            index + value.Length <= text.Length &&
            string.CompareOrdinal(text, index, value, 0, value.Length) == 0;

        static bool IsNameCharacter(char value) =>
            char.IsLetterOrDigit(value) || value == ':' || value == '-' || value == '_' || value == '.';

        static bool IsPunctuation(char value) =>
            value == '/' || value == '=' || value == '?' || value == '!' || value == '[' || value == ']';
    }
}
