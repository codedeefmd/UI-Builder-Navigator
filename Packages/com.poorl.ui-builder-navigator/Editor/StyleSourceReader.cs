using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace UIBuilderUssNavigator
{
    /// <summary>
    /// 读取一个 live 元素的常用计算属性及其声明链。匹配、伪状态、排序、简写展开和文本导出
    /// 都使用 Unity 6.3 原生接口；不依赖 Inspector 当前正在编辑元素还是选择器，也不改文档。
    /// 最终值来自 resolvedStyle，来源指产生该值的样式声明；百分比/auto 的声明不等于布局后的 px。
    /// </summary>
    internal sealed class StyleSourceReader
    {
        const BindingFlags Members = BindingFlags.Public | BindingFlags.NonPublic |
                                     BindingFlags.Instance | BindingFlags.Static;

        readonly object extractor;
        readonly MethodInfo findMatches;
        readonly MethodInfo clearMatches;
        readonly FieldInfo matchRecords;
        readonly FieldInfo recordSheet;
        readonly FieldInfo recordSelector;
        readonly PropertyInfo selectorRule;
        readonly PropertyInfo ruleProperties;
        readonly PropertyInfo propertyName;
        readonly FieldInfo requiresVariable;
        readonly PropertyInfo propertyValues;
        readonly PropertyInfo handleType;
        readonly MethodInfo readColor;
        readonly MethodInfo readEnum;
        readonly MethodInfo longhands;
        readonly MethodInfo selectorText;
        readonly object exporter;
        readonly object exportOptions;
        readonly MethodInfo exportProperty;
        readonly FieldInfo inlineAccess;
        readonly PropertyInfo inlineRule;
        readonly FieldInfo inlineSheet;
        readonly FieldInfo inlineRuleValue;
        readonly MethodInfo isValueSet;
        readonly MethodInfo inlineValue;
        readonly FieldInfo computedStyle;
        readonly FieldInfo matchingRulesHash;
        readonly Dictionary<string, string[]> expanded = new Dictionary<string, string[]>();
        readonly Dictionary<VisualElement, Dictionary<string, List<Declaration>>> elementDeclarations =
            new Dictionary<VisualElement, Dictionary<string, List<Declaration>>>();
        readonly List<Property> properties = new List<Property>();
        VisualElement lastElement;
        string lastSignature;

        internal sealed class Property
        {
            internal string Name;
            internal string GroupKey;
            internal string Group => GroupName(GroupKey);
            internal bool Pixels;
            internal bool Inherited;
            internal object[] IdArguments;
            internal object[] InlineArguments;
            internal BindingId Binding;
            internal PropertyInfo Resolved;
        }

        /// <summary>保留真实 selector 和 property 身份，区分同名规则、逗号分组和同规则内的重复声明。</summary>
        internal sealed class Declaration
        {
            internal VisualElement Owner;
            internal StyleSheet Sheet;
            internal object Selector;
            internal object PropertyIdentity;
            internal string SelectorText;
            internal string Text;
            internal string Keyword;
            internal bool Variable;
            internal bool RuntimeInline;
            internal Color? ColorValue;
            internal bool IsInline => Selector == null;
            internal string File
            {
                get
                {
                    if (Sheet == null) return string.Empty;
                    string path = AssetDatabase.GetAssetPath(Sheet);
                    return path.Length == 0 ? Sheet.name : Path.GetFileName(path);
                }
            }
            internal string Source => IsInline ? RuntimeInline ? L10n.Tr("C# / 运行时内联", "C# / Runtime inline") :
                L10n.Tr("UXML 内联样式", "UXML inline style") :
                SelectorText + "  [" + File + "]";
        }

        internal enum SourceKind { Default, Inherited, Inline, Uss }

        internal sealed class Row
        {
            internal Property Property;
            internal string Value;
            internal Color? ColorValue;
            internal Declaration Winner;
            internal Declaration InheritedSource;
            internal List<Declaration> Declarations;
            // 来源类别参与解析分支，使用稳定枚举；译文仅在视图读取 Kind 时生成。
            internal SourceKind KindKey;
            internal string Kind => KindName(KindKey);
            internal string Note;
            internal bool HasBinding;
        }

        internal StyleSourceReader(Assembly builderAssembly)
        {
            Assembly ui = typeof(VisualElement).Assembly;
            Type matching = ui.GetType("UnityEngine.UIElements.MatchedRulesExtractor", true);
            extractor = Activator.CreateInstance(matching, Members, null, new object[] { null }, null);
            findMatches = Required(matching.GetMethod("FindMatchingRules", Members), "MatchedRulesExtractor.FindMatchingRules");
            clearMatches = Required(matching.GetMethod("Clear", Members), "MatchedRulesExtractor.Clear");
            matchRecords = Required(matching.GetField("matchRecords", Members), "MatchedRulesExtractor.matchRecords");
            Type record = matchRecords.FieldType.GetGenericArguments()[0];
            recordSheet = Required(record.GetField("sheet", Members), "SelectorMatchRecord.sheet");
            recordSelector = Required(record.GetField("complexSelector", Members), "SelectorMatchRecord.complexSelector");
            selectorRule = Required(recordSelector.FieldType.GetProperty("rule", Members), "StyleComplexSelector.rule");
            ruleProperties = Required(selectorRule.PropertyType.GetProperty("properties", Members), "StyleRule.properties");
            Type styleProperty = ruleProperties.PropertyType.GetElementType();
            propertyName = Required(styleProperty.GetProperty("name", Members), "StyleProperty.name");
            requiresVariable = Required(styleProperty.GetField("requireVariableResolve", Members), "StyleProperty.requireVariableResolve");
            propertyValues = Required(styleProperty.GetProperty("values", Members), "StyleProperty.values");
            Type handle = propertyValues.PropertyType.GetElementType();
            handleType = Required(handle.GetProperty("valueType", Members), "StyleValueHandle.valueType");
            readColor = Required(typeof(StyleSheet).GetMethod("ReadColor", Members, null, new[] { handle }, null), "StyleSheet.ReadColor");
            readEnum = Required(typeof(StyleSheet).GetMethods(Members).Single(method =>
                method.Name == "ReadEnum" && !method.IsGenericMethod), "StyleSheet.ReadEnum");

            Type debug = ui.GetType("UnityEngine.UIElements.StyleDebug", true);
            longhands = Required(debug.GetMethod("GetLonghandPropertyNames", Members, null, new[] { typeof(string) }, null),
                "StyleDebug.GetLonghandPropertyNames(string)");
            MethodInfo getId = Required(debug.GetMethod("GetStylePropertyIdFromName", Members), "StyleDebug.GetStylePropertyIdFromName");
            MethodInfo inherited = Required(debug.GetMethod("IsInheritedProperty", Members), "StyleDebug.IsInheritedProperty");
            Type utilities = ui.GetType("UnityEngine.UIElements.StyleSheets.StylePropertyUtil", true);
            var csharpNames = (IDictionary)Required(utilities.GetProperty("ussNameToCSharpName", Members),
                "StylePropertyUtil.ussNameToCSharpName").GetValue(null);

            AddProperties("text", "color,font-size,-unity-font-style,-unity-text-align,white-space", csharpNames, getId, inherited);
            AddProperties("appearance", "background-color,opacity", csharpNames, getId, inherited);
            AddProperties("size-layout", "width,height,min-width,min-height,max-width,max-height,display,position," +
                "flex-direction,flex-grow,flex-shrink,align-items,align-self,justify-content", csharpNames, getId, inherited);
            AddProperties("spacing-border", "margin-top,margin-right,margin-bottom,margin-left,padding-top,padding-right," +
                "padding-bottom,padding-left,border-top-width,border-right-width,border-bottom-width,border-left-width",
                csharpNames, getId, inherited);

            Type builderExporter = builderAssembly.GetType("Unity.UI.Builder.BuilderStyleSheetExporter", true);
            selectorText = Required(builderExporter.GetMethod("GetSelectorString", Members), "BuilderStyleSheetExporter.GetSelectorString");
            exporter = Required(builderExporter.GetProperty("instance", Members), "BuilderStyleSheetExporter.instance").GetValue(null);
            exportOptions = Required(builderExporter.GetField("s_Options", Members), "BuilderStyleSheetExporter.s_Options").GetValue(null);
            exportProperty = Required(builderExporter.BaseType.GetMethod("ToUssString", Members, null,
                new[] { typeof(StyleSheet), styleProperty, exportOptions.GetType() }, null), "StyleSheetExporter.ToUssString");

            inlineAccess = Required(typeof(VisualElement).GetField("inlineStyleAccess", Members), "VisualElement.inlineStyleAccess");
            inlineRule = Required(inlineAccess.FieldType.GetProperty("inlineRule", Members), "InlineStyleAccess.inlineRule");
            inlineSheet = Required(inlineRule.PropertyType.GetField("sheet", Members), "InlineRule.sheet");
            inlineRuleValue = Required(inlineRule.PropertyType.GetField("rule", Members), "InlineRule.rule");
            isValueSet = Required(inlineAccess.FieldType.GetMethod("IsValueSet", Members), "InlineStyleAccess.IsValueSet");
            inlineValue = Required(debug.GetMethod("GetInlineStyleValue", Members, null,
                new[] { typeof(IStyle), typeof(string) }, null), "StyleDebug.GetInlineStyleValue(IStyle,string)");
            computedStyle = Required(typeof(VisualElement).GetField("m_Style", Members), "VisualElement.m_Style");
            matchingRulesHash = Required(computedStyle.FieldType.GetField("matchingRulesHash", Members), "ComputedStyle.matchingRulesHash");
        }

        /// <summary>
        /// 原生 matchingRulesHash 随匹配规则/样式表内容变化；再检查祖先链和受支持属性的运行时内联值。
        /// 因此窗口空闲时不重新匹配规则、导出声明或遍历样式树；布局尺寸仍可独立轻量更新。
        /// 内存预览变化会显式失效，覆盖 UXML 内联规则原对象上修改但匹配 hash 不变的情况。
        /// </summary>
        internal bool NeedsRebuild(VisualElement element, bool sourceChanged)
        {
            var signature = new StringBuilder();
            for (VisualElement current = element; current != null; current = current.parent)
            {
                signature.Append(current.GetHashCode()).Append(':')
                    .Append(matchingRulesHash.GetValue(computedStyle.GetValue(current))).Append(';');
                object access = inlineAccess.GetValue(current);
                foreach (Property property in properties)
                {
                    // 祖先只有继承属性会影响当前元素；布局后的宽高另由 RefreshValues 读取。
                    if (!ReferenceEquals(current, element) && !property.Inherited) continue;
                    if (current.HasBinding(property.Binding))
                        signature.Append(property.Name).Append("=binding;");
                    if (access != null && (bool)isValueSet.Invoke(access, property.IdArguments))
                        signature.Append(property.Name).Append('=')
                            .Append(ReadInlineValue(current, property)).Append(';');
                }
            }
            string state = signature.ToString();
            bool rebuild = sourceChanged || !ReferenceEquals(lastElement, element) || state != lastSignature;
            lastElement = element;
            lastSignature = state;
            return rebuild;
        }

        internal void Invalidate()
        {
            lastElement = null;
            lastSignature = null;
        }

        internal static string GroupName(string key)
        {
            switch (key)
            {
                case "text": return L10n.Tr("文字", "Text");
                case "appearance": return L10n.Tr("外观", "Appearance");
                case "size-layout": return L10n.Tr("尺寸与布局", "Size & Layout");
                default: return L10n.Tr("间距与边框", "Spacing & Border");
            }
        }

        static string KindName(SourceKind kind)
        {
            switch (kind)
            {
                case SourceKind.Default: return L10n.Tr("默认 / 引擎布局", "Default / Engine layout");
                case SourceKind.Inherited: return L10n.Tr("继承", "Inherited");
                case SourceKind.Inline: return L10n.Tr("内联", "Inline");
                default: return "USS";
            }
        }

        void AddProperties(string groupKey, string names, IDictionary csharpNames, MethodInfo getId, MethodInfo inherited)
        {
            foreach (string name in names.Split(','))
            {
                string csharp = (string)csharpNames[name];
                object id = getId.Invoke(null, new object[] { name });
                properties.Add(new Property
                {
                    Name = name, GroupKey = groupKey,
                    IdArguments = new[] { id }, InlineArguments = new object[] { null, name },
                    Binding = new BindingId("style." + csharp),
                    Resolved = Required(typeof(IResolvedStyle).GetProperty(csharp), "IResolvedStyle." + csharp),
                    Inherited = (bool)inherited.Invoke(null, new[] { id }),
                    Pixels = name.Contains("width") || name.Contains("height") || name == "font-size" ||
                             name.StartsWith("margin-") || name.StartsWith("padding-")
                });
            }
        }

        /// <summary>
        /// 一次读取内只匹配每个祖先一次，所有属性共用结果。下一次读取重新匹配，以包含未保存的 USS、
        /// 原生 Inspector 修改和当前伪状态；不缓存到资源生命周期之外，不触发解析/保存/重导入。
        /// </summary>
        internal List<Row> Read(VisualElement element)
        {
            elementDeclarations.Clear();
            var rows = new List<Row>(properties.Count);
            foreach (Property property in properties)
            {
                Row row = Resolve(element, property);
                SetComputedValue(row, property.Resolved.GetValue(element.resolvedStyle));
                row.HasBinding = element.HasBinding(property.Binding);
                if (row.HasBinding)
                    AppendNote(row, L10n.Tr("存在数据绑定；下列展示样式声明顺序，绑定状态可能影响当前值。",
                        "A data binding exists. The declaration order is shown below; binding state may affect the computed value."));
                rows.Add(row);
            }
            elementDeclarations.Clear();
            clearMatches.Invoke(extractor, null);
            return rows;
        }

        internal void RefreshValues(List<Row> rows, VisualElement element)
        {
            foreach (Row row in rows)
                SetComputedValue(row, row.Property.Resolved.GetValue(element.resolvedStyle));
        }

        static void SetComputedValue(Row row, object value)
        {
            row.Value = Format(value, row.Property.Pixels);
            row.ColorValue = value is Color color ? color : (Color?)null;
        }

        Row Resolve(VisualElement element, Property property)
        {
            List<Declaration> declarations = GetDeclarations(element)[property.Name];
            Declaration winner = declarations.LastOrDefault();
            var row = new Row { Property = property, Winner = winner, Declarations = declarations, Note = string.Empty };
            if (winner == null)
            {
                if (property.Inherited && element.parent != null)
                {
                    row = Resolve(element.parent, property);
                    if (row.Winner != null && row.KindKey != SourceKind.Inherited)
                    {
                        row.KindKey = SourceKind.Inherited;
                        string inheritedNote = row.Note;
                        row.Note = L10n.Tr("继承自 ", "Inherited from ") + Describe(row.Winner.Owner) +
                            (inheritedNote.Length == 0 ? L10n.Tr("。", ".") :
                                L10n.Tr("。", ". ") + inheritedNote);
                    }
                    return row;
                }
                row.KindKey = SourceKind.Default;
                row.Note = L10n.Tr("未找到显式声明；数值为当前计算结果。",
                    "No explicit declaration was found; this is the current computed value.");
                return row;
            }

            row.KindKey = winner.IsInline ? SourceKind.Inline : SourceKind.Uss;
            if (winner.Keyword == "unset" && property.Inherited)
            {
                row.Note = L10n.Tr("声明请求继承父级的值。", "The declaration requests the inherited parent value.");
                if (element.parent != null) row.InheritedSource = Resolve(element.parent, property).Winner;
            }
            else if (winner.Keyword == "initial" || winner.Keyword == "unset")
                row.Note = L10n.Tr("声明请求使用该属性的初始值。", "The declaration requests the property's initial value.");
            else if (winner.Keyword == "inherit")
                row.Note = L10n.Tr("该版本的 inherit 应用语义需以计算结果核验，此处不推定父级来源。",
                    "Verify this Unity version's inherit behavior against the computed value; no parent source is inferred here.");
            if (winner.Variable)
                AppendNote(row, L10n.Tr("声明包含变量；跳转定位使用位置，变量定义不在此追踪。",
                    "The declaration contains a variable. Navigation opens the usage; the variable definition is not traced here."));
            return row;
        }

        static void AppendNote(Row row, string note)
        {
            if (row.Note.Length > 0 && L10n.Language == "en") row.Note += " ";
            row.Note += note;
        }

        Dictionary<string, List<Declaration>> GetDeclarations(VisualElement element)
        {
            if (elementDeclarations.TryGetValue(element, out var cached)) return cached;
            var result = properties.ToDictionary(property => property.Name, _ => new List<Declaration>());
            elementDeclarations.Add(element, result);
            clearMatches.Invoke(extractor, null);
            findMatches.Invoke(extractor, new object[] { element });
            // matchRecords 已由原生 SelectorMatchRecord.Compare 排序，包含当前伪状态的真实匹配。
            // 主题、specificity、层级样式表位置和规则顺序都沿用引擎，不借用“相关样式”潜在状态列表。
            foreach (object record in (IEnumerable)matchRecords.GetValue(extractor))
            {
                var sheet = (StyleSheet)recordSheet.GetValue(record);
                object selector = recordSelector.GetValue(record);
                AddRule(result, element, sheet, selector, selectorRule.GetValue(selector));
            }

            // 读取已有 access，避免只为检查而访问 element.style，给无内联值的元素创建新的 access。
            object access = inlineAccess.GetValue(element);
            if (access == null) return result;
            object inline = inlineRule.GetValue(access);
            object rule = inlineRuleValue.GetValue(inline);
            if (rule != null) AddRule(result, element, (StyleSheet)inlineSheet.GetValue(inline), null, rule);
            foreach (Property property in properties)
            {
                if (!(bool)isValueSet.Invoke(access, property.IdArguments)) continue;
                object value = ReadInlineValue(element, property);
                result[property.Name].Add(new Declaration
                {
                    Owner = element, RuntimeInline = true, Text = property.Name + ": " + value + ";",
                    Keyword = value.ToString().ToLowerInvariant(),
                    ColorValue = value is StyleColor color && color.keyword == StyleKeyword.Undefined ? color.value : (Color?)null
                });
            }
            return result;
        }

        object ReadInlineValue(VisualElement element, Property property)
        {
            // Editor 会话在主线程顺序读取，复用参数数组即可；结束后释放对元素 access 的临时引用。
            property.InlineArguments[0] = element.style;
            object value = inlineValue.Invoke(null, property.InlineArguments);
            property.InlineArguments[0] = null;
            return value;
        }

        void AddRule(Dictionary<string, List<Declaration>> result, VisualElement owner, StyleSheet sheet,
            object selector, object rule)
        {
            foreach (object property in (IEnumerable)ruleProperties.GetValue(rule))
            {
                string name = (string)propertyName.GetValue(property);
                if (!expanded.TryGetValue(name, out string[] names))
                {
                    names = result.ContainsKey(name) ? new[] { name } :
                        (string[])longhands.Invoke(null, new object[] { name }) ?? Array.Empty<string>();
                    expanded.Add(name, names);
                }
                if (!names.Any(result.ContainsKey)) continue;
                string text = ((string)exportProperty.Invoke(exporter, new[] { (object)sheet, property, exportOptions })).Trim();
                string value = text.Substring(text.IndexOf(':') + 1).Trim().TrimEnd(';').Trim();
                var declaration = new Declaration
                {
                    Owner = owner, Sheet = sheet, Selector = selector, PropertyIdentity = property,
                    SelectorText = selector == null ? null : (string)selectorText.Invoke(null, new[] { selector }),
                    Text = text, Keyword = value, Variable = (bool)requiresVariable.GetValue(property),
                    ColorValue = LiteralColor(sheet, property)
                };
                foreach (string longhand in names.Where(result.ContainsKey))
                {
                    List<Declaration> list = result[longhand];
                    // 同一条逗号分组规则可能匹配多个选择器；保留最后一次（优先级最高）的匹配。
                    list.RemoveAll(previous => ReferenceEquals(previous.PropertyIdentity, property));
                    list.Add(declaration);
                }
            }
        }

        /// <summary>只为原生 Color 常量提供声明色块；var/initial/unset 不伪造常量，最终颜色另从 resolvedStyle 读取。</summary>
        Color? LiteralColor(StyleSheet sheet, object property)
        {
            var handles = (Array)propertyValues.GetValue(property);
            if (handles.Length != 1) return null;
            object handle = handles.GetValue(0);
            string kind = handleType.GetValue(handle).ToString();
            if (kind == "Color") return (Color)readColor.Invoke(sheet, new[] { handle });
            string name = (string)propertyName.GetValue(property);
            if (kind != "Enum" || name != "color" && name != "background-color") return null;
            string literal = (string)readEnum.Invoke(sheet, new[] { handle });
            if (literal == "transparent") return Color.clear;
            return ColorUtility.TryParseHtmlString(literal, out Color color) ? color : (Color?)null;
        }

        internal static string Describe(VisualElement element) => element.GetType().Name +
            (string.IsNullOrEmpty(element.name) ? string.Empty : " #" + element.name);

        static string Format(object value, bool pixels)
        {
            if (value is float number) return InspectorBoxModel.Format(number) + (pixels ? " px" : string.Empty);
            if (value is Color color)
                return "#" + (Mathf.Approximately(color.a, 1) ? ColorUtility.ToHtmlStringRGB(color) : ColorUtility.ToHtmlStringRGBA(color));
            return Convert.ToString(value, CultureInfo.InvariantCulture);
        }

        static T Required<T>(T member, string name) where T : MemberInfo =>
            member ?? throw new NotSupportedException(L10n.Tr("样式来源接口不兼容：", "Style-source API is incompatible: ") + name);
    }
}
