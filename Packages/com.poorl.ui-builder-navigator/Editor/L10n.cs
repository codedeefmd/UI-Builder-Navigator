using System;
using System.Globalization;
using UnityEditor;
using UnityEngine;

namespace UIBuilderUssNavigator
{
    /// <summary>
    /// 扩展自有界面的中英文选择。译文就近放在使用位置；这里只负责选择、格式化和版本通知，
    /// 不翻译用户源码、USS 属性名、文件名或 Unity 原生 Inspector。偏好独立存入 EditorPrefs，
    /// 切换只增加 Revision，现有窗口按版本更新文字，不销毁控件或保存用户文档。
    /// </summary>
    internal static class L10n
    {
        const string PreferenceKey = "UIBuilderUssNavigator.Language";
        static readonly string DefaultLanguage = Application.systemLanguage.ToString().StartsWith("Chinese", StringComparison.Ordinal)
            ? "zh" : "en";
        internal static string Language { get; private set; } = EditorPrefs.GetString(PreferenceKey, DefaultLanguage);
        internal static int Revision { get; private set; }
        internal static event Action Changed;

        internal static string Tr(string zh, string en) => Language == "zh" ? zh : en;

        /// <summary>只格式化说明中的占位符；布局数值和源码仍沿用各自的原始格式，不受系统文化影响。</summary>
        internal static string Format(string zh, string en, params object[] args) =>
            string.Format(CultureInfo.InvariantCulture, Tr(zh, en), args);

        internal static void SetLanguage(string language)
        {
            if (language != "zh" && language != "en")
                throw new ArgumentException("Supported language codes are zh and en.", nameof(language));
            if (Language == language) return;
            Language = language;
            EditorPrefs.SetString(PreferenceKey, language);
            Revision++;
            Changed?.Invoke();
        }
    }
}
