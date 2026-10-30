using System;
using System.Collections.Generic;
using System.Text;

namespace UIBuilderUssNavigator
{
    internal readonly struct SearchMatch
    {
        internal readonly int Line;
        internal readonly int Column;
        internal readonly int Length;

        internal SearchMatch(int line, int column, int length)
        {
            Line = line;
            Column = column;
            Length = length;
        }
    }

    /// <summary>
    /// 为代码预览提供逐行字面量搜索，并在既有语法富文本上叠加搜索标记。
    /// 列号和长度沿用 .NET 字符串的 UTF-16 索引，能直接对应 ListView 中的原始源码行。
    /// </summary>
    internal static class CodePreviewSearch
    {
        const string NoParseOpen = "<noparse>";
        const string NoParseClose = "</noparse>";

        internal static List<SearchMatch> FindMatches(IReadOnlyList<string> lines, string query)
        {
            var matches = new List<SearchMatch>();
            if (lines == null || string.IsNullOrEmpty(query))
                return matches;

            for (int lineIndex = 0; lineIndex < lines.Count; lineIndex++)
            {
                string line = lines[lineIndex] ?? string.Empty;
                int searchFrom = 0;
                while (searchFrom <= line.Length - query.Length)
                {
                    int column = line.IndexOf(query, searchFrom, StringComparison.OrdinalIgnoreCase);
                    if (column < 0) break;

                    matches.Add(new SearchMatch(lineIndex, column, query.Length));
                    searchFrom = column + query.Length;
                }
            }
            return matches;
        }

        /// <summary>
        /// coloredLine 必须是现有语法高亮器生成的 color/noparse 富文本。
        /// 标记只包在 noparse 外部，查询内容永远不会被解释成 TextCore 标签。
        /// </summary>
        internal static string MarkLine(
            string coloredLine,
            IReadOnlyList<SearchMatch> matches,
            int activeColumn,
            bool darkSkin)
        {
            if (string.IsNullOrEmpty(coloredLine) || matches == null || matches.Count == 0)
                return coloredLine;

            string normalColor = darkSkin ? "FFD54F66" : "FFD54F80";
            string activeColor = darkSkin ? "FF8A00AA" : "FF7A00B3";
            var result = new StringBuilder(coloredLine.Length + matches.Count * 32);
            int richIndex = 0;
            int sourceColumn = 0;
            int matchIndex = 0;

            while (richIndex < coloredLine.Length)
            {
                int literalStart = coloredLine.IndexOf(NoParseOpen, richIndex, StringComparison.Ordinal);
                if (literalStart < 0)
                {
                    result.Append(coloredLine, richIndex, coloredLine.Length - richIndex);
                    break;
                }

                result.Append(coloredLine, richIndex, literalStart - richIndex);
                int contentStart = literalStart + NoParseOpen.Length;
                int contentEnd = coloredLine.IndexOf(NoParseClose, contentStart, StringComparison.Ordinal);
                if (contentEnd < 0)
                    throw new FormatException("Code preview highlighting is missing </noparse>; source columns cannot be mapped.");

                string literal = coloredLine.Substring(contentStart, contentEnd - contentStart);
                AppendMarkedLiteral(result, literal, sourceColumn, matches, ref matchIndex, activeColumn, normalColor, activeColor);
                sourceColumn += literal.Length;
                richIndex = contentEnd + NoParseClose.Length;
            }

            return result.ToString();
        }

        static void AppendMarkedLiteral(
            StringBuilder result,
            string literal,
            int sourceColumn,
            IReadOnlyList<SearchMatch> matches,
            ref int matchIndex,
            int activeColumn,
            string normalColor,
            string activeColor)
        {
            int local = 0;
            int segmentEnd = sourceColumn + literal.Length;
            while (local < literal.Length)
            {
                int globalColumn = sourceColumn + local;
                // FindMatches 按列排序；单调推进游标，长行搜索常见字符时也不反复扫描全部命中。
                while (matchIndex < matches.Count && matches[matchIndex].Column + matches[matchIndex].Length <= globalColumn)
                    matchIndex++;
                if (matchIndex == matches.Count || matches[matchIndex].Column >= segmentEnd)
                {
                    SyntaxText.AppendLiteral(result, literal.Substring(local));
                    break;
                }
                SearchMatch match = matches[matchIndex];
                if (match.Column <= globalColumn)
                {
                    int length = Math.Min(match.Column + match.Length, segmentEnd) - globalColumn;
                    string color = match.Column == activeColumn ? activeColor : normalColor;
                    result.Append("<mark=#").Append(color).Append('>');
                    SyntaxText.AppendLiteral(result, literal.Substring(local, length));
                    result.Append("</mark>");
                    local += length;
                    continue;
                }

                int plainLength = match.Column - globalColumn;
                SyntaxText.AppendLiteral(result, literal.Substring(local, plainLength));
                local += plainLength;
            }
        }

    }
}
