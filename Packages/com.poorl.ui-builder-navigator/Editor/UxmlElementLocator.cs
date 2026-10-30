using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace UIBuilderUssNavigator
{
    /// <summary>
    /// 按 UI Builder Hierarchy 的可见子元素路径，在良构 UXML 中定位对应元素的开始标签。
    /// XmlReader 负责 XML 结构与命名空间语义；本类只补充开始标签末尾的源码行范围计算。
    /// </summary>
    internal static class UxmlElementLocator
    {
        const string UnityElementsNamespace = "UnityEngine.UIElements";

        sealed class ElementFrame
        {
            internal readonly int[] Path;
            internal int NextChildIndex;

            internal ElementFrame(int[] path)
            {
                Path = path;
            }
        }

        /// <summary>
        /// elementPath 从 UXML 根元素开始：空路径表示根，后续数字表示排除 Unity 元数据后的子元素索引。
        /// 返回范围只覆盖开始标签本身，从“&lt;”到不在引号中的“&gt;”，行号从 0 开始且包含两端。
        /// </summary>
        internal static bool TryFind(string text, IReadOnlyList<int> elementPath, out RuleRange range)
        {
            range = default(RuleRange);
            if (string.IsNullOrEmpty(text) || elementPath == null || ContainsNegativeIndex(elementPath))
                return false;

            var settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                IgnoreComments = true,
                IgnoreProcessingInstructions = true
            };

            var frames = new List<ElementFrame>();
            int ignoredMetadataDepth = -1;
            using (var input = new StringReader(text))
            using (XmlReader reader = XmlReader.Create(input, settings))
            {
                while (reader.Read())
                {
                    if (ignoredMetadataDepth >= 0)
                    {
                        if (reader.NodeType == XmlNodeType.EndElement && reader.Depth == ignoredMetadataDepth)
                            ignoredMetadataDepth = -1;
                        continue;
                    }

                    if (reader.NodeType == XmlNodeType.EndElement)
                    {
                        frames.RemoveAt(frames.Count - 1);
                        continue;
                    }

                    if (reader.NodeType != XmlNodeType.Element)
                        continue;

                    if (IsUnityMetadata(reader.LocalName, reader.NamespaceURI))
                    {
                        if (!reader.IsEmptyElement)
                            ignoredMetadataDepth = reader.Depth;
                        continue;
                    }

                    int[] currentPath = CreatePath(frames);
                    if (PathsEqual(currentPath, elementPath))
                    {
                        IXmlLineInfo lineInfo = (IXmlLineInfo)reader;
                        int startLine = lineInfo.LineNumber - 1;
                        int openingBracket = GetOpeningBracketIndex(text, startLine, lineInfo.LinePosition);
                        int tagEnd = FindStartTagEnd(text, openingBracket + 1);
                        if (tagEnd < 0)
                            return false;

                        range = new RuleRange(startLine, GetZeroBasedLine(text, tagEnd));
                        return true;
                    }

                    if (!reader.IsEmptyElement)
                        frames.Add(new ElementFrame(currentPath));
                }
            }

            return false;
        }

        static int[] CreatePath(List<ElementFrame> frames)
        {
            if (frames.Count == 0)
                return Array.Empty<int>();

            ElementFrame parent = frames[frames.Count - 1];
            int[] path = new int[parent.Path.Length + 1];
            Array.Copy(parent.Path, path, parent.Path.Length);
            path[path.Length - 1] = parent.NextChildIndex++;
            return path;
        }

        static bool IsUnityMetadata(string localName, string namespaceUri)
        {
            bool unityNamespace = namespaceUri.Length == 0 ||
                string.Equals(namespaceUri, UnityElementsNamespace, StringComparison.Ordinal);
            if (!unityNamespace)
                return false;

            return localName == "Style" || localName == "Template" || localName == "AttributeOverrides";
        }

        static int GetOpeningBracketIndex(string text, int line, int linePosition)
        {
            int lineStart = GetLineStart(text, line);
            // XmlReader 的列号从 1 开始并指向元素名首字符，前一个字符就是开始括号。
            return lineStart + linePosition - 2;
        }

        static int FindStartTagEnd(string text, int index)
        {
            char quote = '\0';
            while (index < text.Length)
            {
                char current = text[index];
                if (quote != '\0')
                {
                    if (current == quote) quote = '\0';
                }
                else if (current == '\'' || current == '"')
                {
                    quote = current;
                }
                else if (current == '>')
                {
                    return index;
                }
                index++;
            }
            return -1;
        }

        static int GetLineStart(string text, int targetLine)
        {
            int line = 0;
            int index = 0;
            while (line < targetLine && index < text.Length)
            {
                if (text[index] == '\r')
                {
                    index++;
                    if (index < text.Length && text[index] == '\n') index++;
                    line++;
                }
                else if (text[index++] == '\n')
                {
                    line++;
                }
            }
            return index;
        }

        static int GetZeroBasedLine(string text, int characterIndex)
        {
            int line = 0;
            int index = 0;
            while (index < characterIndex)
            {
                if (text[index] == '\r')
                {
                    line++;
                    if (index + 1 < characterIndex && text[index + 1] == '\n') index++;
                }
                else if (text[index] == '\n')
                {
                    line++;
                }
                index++;
            }
            return line;
        }

        static bool PathsEqual(int[] actual, IReadOnlyList<int> expected)
        {
            if (actual.Length != expected.Count) return false;
            for (int index = 0; index < actual.Length; index++)
                if (actual[index] != expected[index]) return false;
            return true;
        }

        static bool ContainsNegativeIndex(IReadOnlyList<int> path)
        {
            for (int index = 0; index < path.Count; index++)
                if (path[index] < 0) return true;
            return false;
        }
    }
}
