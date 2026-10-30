using System;
using System.Text;

namespace UIBuilderUssNavigator
{
    /// <summary>
    /// 诊断面板中的语义文字角色。角色只描述“这段文字是什么”，不承担 USS 解析或样式来源判定。
    /// </summary>
    internal enum DiagnosticTone
    {
        Text,
        Property,
        Number,
        Unit,
        Keyword,
        Type,
        Name,
        Selector,
        File,
        Comment,
        Function
    }

    /// <summary>
    /// 为 Inspector 只读诊断信息提供低饱和度语义配色。
    /// 输入均视为待显示的原文：本类只做轻量词法分段，不修改字符，也不尝试成为完整的 CSS/USS 解析器。
    /// </summary>
    internal static class DiagnosticText
    {
        /// <summary>返回不含井号的六位 RGB 色值，便于富文本和只读色块共用同一套角色颜色。</summary>
        internal static string ColorCode(DiagnosticTone tone, bool darkSkin)
        {
            if (darkSkin)
            {
                switch (tone)
                {
                    case DiagnosticTone.Text: return "C1CAD6";
                    case DiagnosticTone.Property: return "93C6DE";
                    case DiagnosticTone.Number: return "BFD6AF";
                    case DiagnosticTone.Unit: return "8CABB6";
                    case DiagnosticTone.Keyword: return "CAA5D7";
                    case DiagnosticTone.Type: return "D6C695";
                    case DiagnosticTone.Name: return "DCB696";
                    case DiagnosticTone.Selector: return "8FCBB9";
                    case DiagnosticTone.File: return "9BACC0";
                    case DiagnosticTone.Comment: return "9AA6B4";
                    case DiagnosticTone.Function: return "D6C695";
                }
            }
            else
            {
                switch (tone)
                {
                    case DiagnosticTone.Text: return "394757";
                    case DiagnosticTone.Property: return "005A7A";
                    case DiagnosticTone.Number: return "3F641C";
                    case DiagnosticTone.Unit: return "486874";
                    case DiagnosticTone.Keyword: return "704580";
                    case DiagnosticTone.Type: return "6B5700";
                    case DiagnosticTone.Name: return "7A4A28";
                    case DiagnosticTone.Selector: return "006B58";
                    case DiagnosticTone.File: return "4D5D70";
                    case DiagnosticTone.Comment: return "5E6B79";
                    case DiagnosticTone.Function: return "6B5700";
                }
            }

            throw new ArgumentOutOfRangeException(nameof(tone), tone, null);
        }

        /// <summary>
        /// 将一段原文写进单一颜色范围。动态内容统一经过 SyntaxText.AppendLiteral，避免原文中的富文本标签被执行。
        /// </summary>
        internal static string Colorize(string text, DiagnosticTone tone, bool darkSkin)
        {
            var result = new StringBuilder(text.Length + 48);
            AppendColored(result, text, tone, darkSkin);
            return result.ToString();
        }

        /// <summary>
        /// 对 USS 属性值做轻量分层：数字、单位、枚举值、变量名、函数和字符串使用不同角色，其余字符保持原文颜色。
        /// Unity 的枚举值并非固定 CSS 关键字集合，因此非函数标识符统一视为 Keyword，避免未知枚举退回低对比度正文色。
        /// 字符串复用 Name 角色，因为公开角色集合没有单独的 String；这仅影响显示，不改变数据含义。
        /// </summary>
        internal static string Value(string text, bool darkSkin)
        {
            var result = new StringBuilder(text.Length * 2);
            int position = 0;
            while (position < text.Length)
            {
                char current = text[position];
                if (current == '\'' || current == '"')
                {
                    int end = ScanString(text, position);
                    AppendColored(result, text.Substring(position, end - position), DiagnosticTone.Name, darkSkin);
                    position = end;
                    continue;
                }

                int hexEnd;
                if (current == '#' && TryScanHex(text, position, out hexEnd))
                {
                    AppendColored(result, text.Substring(position, hexEnd - position), DiagnosticTone.Number, darkSkin);
                    position = hexEnd;
                    continue;
                }

                if (current == '%')
                {
                    AppendColored(result, "%", DiagnosticTone.Unit, darkSkin);
                    position++;
                    continue;
                }

                int numberEnd;
                if (TryScanNumber(text, position, out numberEnd))
                {
                    AppendColored(result, text.Substring(position, numberEnd - position), DiagnosticTone.Number, darkSkin);
                    position = numberEnd;

                    int unitEnd = position;
                    if (unitEnd < text.Length && text[unitEnd] == '%')
                        unitEnd++;
                    else
                        while (unitEnd < text.Length && char.IsLetter(text[unitEnd])) unitEnd++;

                    if (unitEnd > position)
                    {
                        AppendColored(result, text.Substring(position, unitEnd - position), DiagnosticTone.Unit, darkSkin);
                        position = unitEnd;
                    }
                    continue;
                }

                if (IsIdentifierStart(current))
                {
                    int end = position + 1;
                    while (end < text.Length && IsIdentifierPart(text[end])) end++;
                    string token = text.Substring(position, end - position);
                    DiagnosticTone tone = end < text.Length && text[end] == '('
                        ? DiagnosticTone.Function
                        : token.StartsWith("--", StringComparison.Ordinal) ? DiagnosticTone.Name
                        : IsUnit(token) ? DiagnosticTone.Unit : DiagnosticTone.Keyword;
                    AppendColored(result, token, tone, darkSkin);
                    position = end;
                    continue;
                }

                AppendColored(result, text.Substring(position, 1), DiagnosticTone.Text, darkSkin);
                position++;
            }
            return result.ToString();
        }

        /// <summary>突出显示元素类型与井号名称；空白和未识别的附加信息仍逐字保留。</summary>
        internal static string Identity(string text, bool darkSkin)
        {
            var result = new StringBuilder(text.Length * 2);
            int position = 0;
            bool typeWritten = false;
            while (position < text.Length)
            {
                if (char.IsWhiteSpace(text[position]))
                {
                    int end = position + 1;
                    while (end < text.Length && char.IsWhiteSpace(text[end])) end++;
                    AppendColored(result, text.Substring(position, end - position), DiagnosticTone.Text, darkSkin);
                    position = end;
                    continue;
                }

                int tokenEnd = position + 1;
                while (tokenEnd < text.Length && !char.IsWhiteSpace(text[tokenEnd])) tokenEnd++;
                DiagnosticTone tone;
                if (text[position] == '#')
                    tone = DiagnosticTone.Name;
                else if (!typeWritten)
                {
                    tone = DiagnosticTone.Type;
                    typeWritten = true;
                }
                else
                    tone = DiagnosticTone.Text;

                AppendColored(result, text.Substring(position, tokenEnd - position), tone, darkSkin);
                position = tokenEnd;
            }
            return result.ToString();
        }

        /// <summary>
        /// 格式化一条“属性: 值”声明。只使用第一个冒号划分属性和值，不解释嵌套语法；没有冒号时按普通值显示。
        /// </summary>
        internal static string Declaration(string text, bool darkSkin)
        {
            int colon = text.IndexOf(':');
            if (colon < 0)
                return Value(text, darkSkin);

            var result = new StringBuilder(text.Length * 2);
            int propertyStart = 0;
            while (propertyStart < colon && char.IsWhiteSpace(text[propertyStart])) propertyStart++;
            int propertyEnd = colon;
            while (propertyEnd > propertyStart && char.IsWhiteSpace(text[propertyEnd - 1])) propertyEnd--;

            if (propertyStart > 0)
                AppendColored(result, text.Substring(0, propertyStart), DiagnosticTone.Text, darkSkin);
            if (propertyEnd > propertyStart)
                AppendColored(result, text.Substring(propertyStart, propertyEnd - propertyStart), DiagnosticTone.Property, darkSkin);
            if (propertyEnd < colon)
                AppendColored(result, text.Substring(propertyEnd, colon - propertyEnd), DiagnosticTone.Text, darkSkin);
            AppendColored(result, ":", DiagnosticTone.Text, darkSkin);
            result.Append(Value(text.Substring(colon + 1), darkSkin));
            return result.ToString();
        }

        static void AppendColored(StringBuilder result, string text, DiagnosticTone tone, bool darkSkin)
        {
            result.Append("<color=#").Append(ColorCode(tone, darkSkin)).Append('>');
            SyntaxText.AppendLiteral(result, text);
            result.Append("</color>");
        }

        static int ScanString(string text, int start)
        {
            char quote = text[start];
            int position = start + 1;
            while (position < text.Length)
            {
                if (text[position] == '\\' && position + 1 < text.Length)
                {
                    position += 2;
                    continue;
                }
                if (text[position++] == quote) break;
            }
            return position;
        }

        static bool TryScanHex(string text, int start, out int end)
        {
            end = start + 1;
            while (end < text.Length && IsHexDigit(text[end])) end++;
            int digits = end - start - 1;
            return digits == 3 || digits == 4 || digits == 6 || digits == 8;
        }

        static bool TryScanNumber(string text, int start, out int end)
        {
            int position = start;
            if (position < text.Length && (text[position] == '+' || text[position] == '-')) position++;

            int integerStart = position;
            while (position < text.Length && char.IsDigit(text[position])) position++;
            bool hasDigits = position > integerStart;

            if (position < text.Length && text[position] == '.')
            {
                position++;
                int fractionStart = position;
                while (position < text.Length && char.IsDigit(text[position])) position++;
                hasDigits |= position > fractionStart;
            }

            if (!hasDigits)
            {
                end = start;
                return false;
            }

            if (position < text.Length && (text[position] == 'e' || text[position] == 'E'))
            {
                int exponent = position++;
                if (position < text.Length && (text[position] == '+' || text[position] == '-')) position++;
                int exponentDigits = position;
                while (position < text.Length && char.IsDigit(text[position])) position++;
                if (position == exponentDigits) position = exponent;
            }

            end = position;
            return true;
        }

        static bool IsIdentifierStart(char value) => char.IsLetter(value) || value == '_' || value == '-';
        static bool IsIdentifierPart(char value) => char.IsLetterOrDigit(value) || value == '_' || value == '-';
        static bool IsUnit(string value)
        {
            switch (value.ToLowerInvariant())
            {
                case "px":
                case "pt":
                case "pc":
                case "cm":
                case "mm":
                case "in":
                case "em":
                case "rem":
                case "ex":
                case "ch":
                case "vw":
                case "vh":
                case "vmin":
                case "vmax":
                case "deg":
                case "rad":
                case "grad":
                case "turn":
                case "ms":
                case "s":
                    return true;
                default:
                    return false;
            }
        }

        static bool IsHexDigit(char value) =>
            value >= '0' && value <= '9' || value >= 'a' && value <= 'f' || value >= 'A' && value <= 'F';
    }
}
