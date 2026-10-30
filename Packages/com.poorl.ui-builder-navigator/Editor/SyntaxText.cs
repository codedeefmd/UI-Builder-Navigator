namespace UIBuilderUssNavigator
{
    /// <summary>
    /// 为 TextCore 富文本预览写入逐字显示的源码片段。
    /// 两种语法高亮器共享此边界，避免各自实现时对源码中的富文本外观产生不同的转义行为。
    /// </summary>
    internal static class SyntaxText
    {
        /// <summary>
        /// noparse 会保护源码里的 &lt;color&gt; 等文本，但源码本身也可能包含大小写任意的 &lt;/noparse&gt;。
        /// 遇到结束标记时，将“&lt;”和剩余字符放进两个独立段；显示结果仍与源码逐字相同。
        /// </summary>
        internal static void AppendLiteral(System.Text.StringBuilder result, string text)
        {
            int position = 0;
            while (position < text.Length)
            {
                int closing = text.IndexOf("</noparse>", position, System.StringComparison.OrdinalIgnoreCase);
                if (closing < 0)
                {
                    result.Append("<noparse>").Append(text, position, text.Length - position).Append("</noparse>");
                    return;
                }

                if (closing > position)
                    result.Append("<noparse>").Append(text, position, closing - position).Append("</noparse>");

                result.Append("<noparse><</noparse><noparse>")
                    .Append(text, closing + 1, 9)
                    .Append("</noparse>");
                position = closing + 10;
            }
        }
    }
}
