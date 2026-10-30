using System;
using System.Collections.Generic;
using System.Text;

namespace UIBuilderUssNavigator
{
    /// <summary>
    /// 将普通 Unity USS 按行转换为 TextCore 富文本，只负责预览配色，不修改或重新格式化源码。
    /// 扫描器保留跨行注释和字符串状态；每一行的富文本标签则独立闭合，便于虚拟列表任意复用行控件。
    /// </summary>
    internal static class UssSyntaxHighlighter
    {
        readonly struct Palette
        {
            internal readonly string Selector;
            internal readonly string Property;
            internal readonly string String;
            internal readonly string Number;
            internal readonly string Function;
            internal readonly string Keyword;
            internal readonly string Comment;
            internal readonly string Punctuation;

            internal Palette(string selector, string property, string text, string number,
                string function, string keyword, string comment, string punctuation)
            {
                Selector = selector;
                Property = property;
                String = text;
                Number = number;
                Function = function;
                Keyword = keyword;
                Comment = comment;
                Punctuation = punctuation;
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
            internal char Quote;
            internal bool StringEscaped;
            internal int RuleDepth;
            internal bool ExpectProperty;
        }

        static readonly Palette Dark = new Palette(
            "4EC9B0", "9CDCFE", "CE9178", "B5CEA8", "DCDCAA", "C586C0", "6A9955", "D4D4D4");

        static readonly Palette Light = new Palette(
            "267F99", "0451A5", "A31515", "098658", "795E26", "AF00DB", "008000", "505050");

        /// <summary>
        /// 返回与输入数量完全一致的高亮行。每次调用都创建新的词法状态，多个文档之间不会串联注释或字符串。
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
                    int end = line.IndexOf("*/", index, StringComparison.Ordinal);
                    if (end < 0)
                    {
                        Add(spans, line.Substring(index), palette.Comment);
                        index = line.Length;
                        continue;
                    }

                    Add(spans, line.Substring(index, end + 2 - index), palette.Comment);
                    state.InComment = false;
                    index = end + 2;
                    continue;
                }

                if (state.Quote != '\0')
                {
                    bool closed;
                    int end = FindQuoteEnd(line, index, state.Quote, ref state.StringEscaped, out closed);
                    Add(spans, line.Substring(index, end - index), palette.String);
                    if (closed)
                    {
                        state.Quote = '\0';
                        state.StringEscaped = false;
                    }
                    index = end;
                    continue;
                }

                if (StartsWith(line, index, "/*"))
                {
                    int end = line.IndexOf("*/", index + 2, StringComparison.Ordinal);
                    if (end < 0)
                    {
                        Add(spans, line.Substring(index), palette.Comment);
                        state.InComment = true;
                        break;
                    }

                    Add(spans, line.Substring(index, end + 2 - index), palette.Comment);
                    index = end + 2;
                    continue;
                }

                char current = line[index];
                if (current == '\'' || current == '"')
                {
                    state.Quote = current;
                    state.StringEscaped = false;
                    bool closed;
                    int end = FindQuoteEnd(line, index + 1, current, ref state.StringEscaped, out closed);
                    Add(spans, line.Substring(index, end - index), palette.String);
                    if (closed)
                    {
                        state.Quote = '\0';
                        state.StringEscaped = false;
                    }
                    index = end;
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

                if (current == '{')
                {
                    Add(spans, "{", palette.Punctuation);
                    state.RuleDepth++;
                    state.ExpectProperty = true;
                    index++;
                    continue;
                }

                if (current == '}')
                {
                    Add(spans, "}", palette.Punctuation);
                    if (state.RuleDepth > 0) state.RuleDepth--;
                    state.ExpectProperty = state.RuleDepth > 0;
                    index++;
                    continue;
                }

                if (state.RuleDepth == 0)
                {
                    index = ScanSelector(line, index, spans, palette);
                    continue;
                }

                if (state.ExpectProperty)
                {
                    if (current == ';')
                    {
                        Add(spans, ";", palette.Punctuation);
                        index++;
                        continue;
                    }

                    int propertyEnd = index;
                    while (propertyEnd < line.Length && IsPropertyCharacter(line[propertyEnd])) propertyEnd++;
                    if (propertyEnd > index)
                    {
                        Add(spans, line.Substring(index, propertyEnd - index), palette.Property);
                        index = propertyEnd;
                        continue;
                    }

                    if (current == ':')
                    {
                        Add(spans, ":", palette.Punctuation);
                        state.ExpectProperty = false;
                        index++;
                        continue;
                    }
                }

                index = ScanValue(line, index, spans, state, palette);
            }

            return Render(spans);
        }

        static int ScanSelector(string line, int index, List<Span> spans, Palette palette)
        {
            char current = line[index];
            if (IsPunctuation(current))
            {
                Add(spans, current.ToString(), palette.Punctuation);
                return index + 1;
            }

            int end = index + 1;
            while (end < line.Length && !char.IsWhiteSpace(line[end]) && line[end] != '{' &&
                   !IsPunctuation(line[end]) && !StartsWith(line, end, "/*")) end++;
            string token = line.Substring(index, end - index);
            int next = end;
            while (next < line.Length && char.IsWhiteSpace(line[next])) next++;
            string color = token[0] == '@'
                ? palette.Keyword
                : next < line.Length && line[next] == '(' ? palette.Function : palette.Selector;
            Add(spans, token, color);
            return end;
        }

        static int ScanValue(string line, int index, List<Span> spans, ScanState state, Palette palette)
        {
            char current = line[index];
            if (current == ';')
            {
                Add(spans, ";", palette.Punctuation);
                state.ExpectProperty = true;
                return index + 1;
            }

            if (IsNumberStart(line, index))
            {
                int end = index + 1;
                while (end < line.Length && (char.IsLetterOrDigit(line[end]) ||
                    line[end] == '.' || line[end] == '%' || line[end] == '+' || line[end] == '-')) end++;
                Add(spans, line.Substring(index, end - index), palette.Number);
                return end;
            }

            if (current == '#')
            {
                int end = index + 1;
                while (end < line.Length && IsHex(line[end])) end++;
                Add(spans, line.Substring(index, end - index), palette.Number);
                return end;
            }

            if (IsIdentifierStart(current))
            {
                int end = index + 1;
                while (end < line.Length && IsIdentifierCharacter(line[end])) end++;
                int next = end;
                while (next < line.Length && char.IsWhiteSpace(line[next])) next++;
                Add(spans, line.Substring(index, end - index),
                    next < line.Length && line[next] == '(' ? palette.Function : palette.Keyword);
                return end;
            }

            Add(spans, current.ToString(), palette.Punctuation);
            return index + 1;
        }

        static int FindQuoteEnd(string line, int index, char quote, ref bool escaped, out bool closed)
        {
            closed = false;
            while (index < line.Length)
            {
                char current = line[index++];
                if (escaped)
                {
                    escaped = false;
                    continue;
                }
                if (current == '\\')
                {
                    escaped = true;
                    continue;
                }
                if (current == quote)
                {
                    closed = true;
                    break;
                }
            }
            return index;
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
            index + value.Length <= text.Length && string.CompareOrdinal(text, index, value, 0, value.Length) == 0;

        static bool IsPropertyCharacter(char value) => char.IsLetterOrDigit(value) || value == '-' || value == '_';
        static bool IsIdentifierStart(char value) => char.IsLetter(value) || value == '-' || value == '_';
        static bool IsIdentifierCharacter(char value) => char.IsLetterOrDigit(value) || value == '-' || value == '_';
        static bool IsPunctuation(char value) => value == ',' || value == '>' || value == '+' || value == '~' ||
                                                   value == ':' || value == '(' || value == ')' || value == '[' ||
                                                   value == ']' || value == '=' || value == ';';
        static bool IsNumberStart(string text, int index) => char.IsDigit(text[index]) ||
            (text[index] == '.' && index + 1 < text.Length && char.IsDigit(text[index + 1])) ||
            ((text[index] == '+' || text[index] == '-') && index + 1 < text.Length && char.IsDigit(text[index + 1]));
        static bool IsHex(char value) => char.IsDigit(value) || value >= 'a' && value <= 'f' || value >= 'A' && value <= 'F';
    }
}
