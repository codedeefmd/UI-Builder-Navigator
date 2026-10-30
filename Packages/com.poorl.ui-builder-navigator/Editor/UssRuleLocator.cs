using System;
using System.Collections.Generic;
using System.Text;

namespace UIBuilderUssNavigator
{
    /// <summary>
    /// 表示 USS Preview 文本中一条完整规则占用的行范围。
    /// 行号从 0 开始，起止行均包含在范围内，便于编辑器直接定位并选中整条规则。
    /// </summary>
    internal readonly struct RuleRange
    {
        public readonly int StartLine;
        public readonly int EndLine;

        public RuleRange(int startLine, int endLine)
        {
            StartLine = startLine;
            EndLine = endLine;
        }
    }

    /// <summary>
    /// 在 UI Builder 生成的 USS Preview 文本中定位顶层样式规则。
    /// 这里只处理普通顶层规则，不尝试实现完整 CSS 语法，也不会进入 @media 等 at-rule 的内部继续查找。
    /// </summary>
    internal static class UssRuleLocator
    {
        /// <summary>
        /// 查找第 occurrence 条包含指定选择器的完整规则。
        /// occurrence 从 0 开始计数；返回范围的 StartLine/EndLine 同样是从 0 开始且包含边界。
        /// </summary>
        public static bool TryFind(string previewText, string selector, int occurrence, out RuleRange range)
        {
            range = default(RuleRange);

            if (string.IsNullOrWhiteSpace(previewText) || string.IsNullOrWhiteSpace(selector) || occurrence < 0)
            {
                return false;
            }

            string normalizedTarget = NormalizeSelector(selector);
            if (normalizedTarget.Length == 0)
            {
                return false;
            }

            int matchedRules = 0;
            int segmentStart = 0;
            int index = 0;

            // 扫描器只识别顶层的分号和花括号。注释、引号及转义必须在这里跳过，
            // 否则声明值里的 "}" 或注释里的伪规则会被误判成真实规则边界。
            while (index < previewText.Length)
            {
                char current = previewText[index];

                if (current == '/' && index + 1 < previewText.Length && previewText[index + 1] == '*')
                {
                    index = SkipComment(previewText, index + 2);
                    continue;
                }

                if (current == '\'' || current == '"')
                {
                    index = SkipQuotedText(previewText, index + 1, current);
                    continue;
                }

                if (current == ';')
                {
                    // @import 等无块 at-rule 以分号结束；下一条规则的头部不能把它包含进来。
                    segmentStart = index + 1;
                    index++;
                    continue;
                }

                if (current == '{')
                {
                    int closingBrace = FindBlockEnd(previewText, index);
                    if (closingBrace < 0)
                    {
                        // 没有闭合花括号就不存在“完整规则”，因此不能返回部分范围。
                        return false;
                    }

                    int headerStart = FindFirstHeaderToken(previewText, segmentStart, index);
                    if (headerStart >= 0)
                    {
                        string header = previewText.Substring(headerStart, index - headerStart);

                        // at-rule 整块跳过。当前插件只负责普通 USS 选择器，不猜测嵌套规则语义。
                        if (!StartsWithAtRule(header) && HeaderContainsSelector(header, normalizedTarget))
                        {
                            if (matchedRules == occurrence)
                            {
                                range = new RuleRange(
                                    GetZeroBasedLine(previewText, headerStart),
                                    GetZeroBasedLine(previewText, closingBrace));
                                return true;
                            }

                            matchedRules++;
                        }
                    }

                    index = closingBrace + 1;
                    segmentStart = index;
                    continue;
                }

                if (current == '}')
                {
                    // 容忍顶层孤立右括号，并从其后重新开始收集规则头部。
                    segmentStart = index + 1;
                }

                index++;
            }

            return false;
        }

        private static bool HeaderContainsSelector(string header, string normalizedTarget)
        {
            foreach (string selector in SplitSelectorGroup(header))
            {
                if (string.Equals(NormalizeSelector(selector), normalizedTarget, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static IEnumerable<string> SplitSelectorGroup(string header)
        {
            int partStart = 0;
            int bracketDepth = 0;
            int parenthesisDepth = 0;
            int index = 0;

            while (index < header.Length)
            {
                char current = header[index];

                if (current == '/' && index + 1 < header.Length && header[index + 1] == '*')
                {
                    index = SkipComment(header, index + 2);
                    continue;
                }

                if (current == '\'' || current == '"')
                {
                    index = SkipQuotedText(header, index + 1, current);
                    continue;
                }

                if (current == '\\' && index + 1 < header.Length)
                {
                    index += 2;
                    continue;
                }

                if (current == '[')
                {
                    bracketDepth++;
                }
                else if (current == ']' && bracketDepth > 0)
                {
                    bracketDepth--;
                }
                else if (current == '(')
                {
                    parenthesisDepth++;
                }
                else if (current == ')' && parenthesisDepth > 0)
                {
                    parenthesisDepth--;
                }
                else if (current == ',' && bracketDepth == 0 && parenthesisDepth == 0)
                {
                    yield return header.Substring(partStart, index - partStart);
                    partStart = index + 1;
                }

                index++;
            }

            yield return header.Substring(partStart);
        }

        private static string NormalizeSelector(string selector)
        {
            var normalized = new StringBuilder(selector.Length);
            bool pendingWhitespace = false;
            int index = 0;

            while (index < selector.Length)
            {
                char current = selector[index];

                if (current == '/' && index + 1 < selector.Length && selector[index + 1] == '*')
                {
                    pendingWhitespace = true;
                    index = SkipComment(selector, index + 2);
                    continue;
                }

                if (char.IsWhiteSpace(current))
                {
                    pendingWhitespace = true;
                    index++;
                    continue;
                }

                if (current == '\'' || current == '"')
                {
                    AppendPendingWhitespace(normalized, ref pendingWhitespace, current);
                    int quotedEnd = SkipQuotedText(selector, index + 1, current);
                    normalized.Append(selector, index, quotedEnd - index);
                    index = quotedEnd;
                    continue;
                }

                if (IsCombinator(current))
                {
                    TrimTrailingWhitespace(normalized);
                    normalized.Append(current);
                    pendingWhitespace = false;
                    index++;
                    continue;
                }

                AppendPendingWhitespace(normalized, ref pendingWhitespace, current);
                normalized.Append(current);
                index++;
            }

            return normalized.ToString().Trim();
        }

        private static void AppendPendingWhitespace(StringBuilder builder, ref bool pendingWhitespace, char nextCharacter)
        {
            if (pendingWhitespace && builder.Length > 0 && !IsCombinator(builder[builder.Length - 1]) && !IsCombinator(nextCharacter))
            {
                builder.Append(' ');
            }

            pendingWhitespace = false;
        }

        private static void TrimTrailingWhitespace(StringBuilder builder)
        {
            while (builder.Length > 0 && char.IsWhiteSpace(builder[builder.Length - 1]))
            {
                builder.Length--;
            }
        }

        private static bool IsCombinator(char value)
        {
            return value == '>' || value == '+' || value == '~';
        }

        private static bool StartsWithAtRule(string header)
        {
            int token = FindFirstHeaderToken(header, 0, header.Length);
            return token >= 0 && header[token] == '@';
        }

        private static int FindFirstHeaderToken(string text, int start, int endExclusive)
        {
            int index = start;

            while (index < endExclusive)
            {
                if (char.IsWhiteSpace(text[index]))
                {
                    index++;
                    continue;
                }

                if (text[index] == '/' && index + 1 < endExclusive && text[index + 1] == '*')
                {
                    index = SkipComment(text, index + 2);
                    continue;
                }

                return index < endExclusive ? index : -1;
            }

            return -1;
        }

        private static int FindBlockEnd(string text, int openingBrace)
        {
            int depth = 1;
            int index = openingBrace + 1;

            while (index < text.Length)
            {
                char current = text[index];

                if (current == '/' && index + 1 < text.Length && text[index + 1] == '*')
                {
                    index = SkipComment(text, index + 2);
                    continue;
                }

                if (current == '\'' || current == '"')
                {
                    index = SkipQuotedText(text, index + 1, current);
                    continue;
                }

                if (current == '{')
                {
                    depth++;
                }
                else if (current == '}' && --depth == 0)
                {
                    return index;
                }

                index++;
            }

            return -1;
        }

        private static int SkipComment(string text, int contentStart)
        {
            int index = contentStart;
            while (index + 1 < text.Length)
            {
                if (text[index] == '*' && text[index + 1] == '/')
                {
                    return index + 2;
                }

                index++;
            }

            return text.Length;
        }

        private static int SkipQuotedText(string text, int contentStart, char quote)
        {
            int index = contentStart;
            while (index < text.Length)
            {
                if (text[index] == '\\' && index + 1 < text.Length)
                {
                    index += 2;
                    continue;
                }

                if (text[index] == quote)
                {
                    return index + 1;
                }

                index++;
            }

            return text.Length;
        }

        private static int GetZeroBasedLine(string text, int characterIndex)
        {
            int line = 0;
            int index = 0;

            while (index < characterIndex)
            {
                if (text[index] == '\r')
                {
                    line++;
                    if (index + 1 < characterIndex && text[index + 1] == '\n')
                    {
                        index++;
                    }
                }
                else if (text[index] == '\n')
                {
                    line++;
                }

                index++;
            }

            return line;
        }
    }
}
