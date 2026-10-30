using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;

namespace UIBuilderUssNavigator
{
    /// <summary>
    /// 注册可翻译的 Unity 顶部菜单，并在扩展关闭时保留语言入口。MenuItem 特性的路径是编译期常量，
    /// 因此本类将 Unity 6.3 的动态菜单接口限制在一个边界内；仅移除自己记录的路径，重载前释放。
    /// 菜单名称变化不作为功能身份，启用状态和语言偏好仍由各自的设置所有者维护。
    /// </summary>
    internal static class NavigatorMenu
    {
        const string Root = "Window/UI Toolkit/USS Preview Navigator/";
        const BindingFlags Members = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        static readonly MethodInfo AddItem = Required("AddMenuItem");
        static readonly MethodInfo RemoveItem = Required("RemoveMenuItem");
        static readonly List<string> paths = new List<string>();
        static Action toggle;
        static Func<bool> isEnabled;

        internal static void Initialize(Action toggleAction, Func<bool> enabled)
        {
            toggle = toggleAction;
            isEnabled = enabled;
            L10n.Changed += ScheduleRebuild;
            AssemblyReloadEvents.beforeAssemblyReload += Remove;
            EditorApplication.quitting += Remove;
            ScheduleRebuild();
        }

        // 不在菜单动作回调内部修改正在展开的菜单；合并到下一次 Editor delayCall。
        static void ScheduleRebuild()
        {
            EditorApplication.delayCall -= Rebuild;
            EditorApplication.delayCall += Rebuild;
        }

        static void Rebuild()
        {
            Remove();
            Add(Root + L10n.Tr("启用", "Enabled"), 100, toggle, isEnabled);
            string languageRoot = Root + L10n.Tr("语言/", "Language/");
            Add(languageRoot + "中文", 120, () => L10n.SetLanguage("zh"), () => L10n.Language == "zh");
            Add(languageRoot + "English", 121, () => L10n.SetLanguage("en"), () => L10n.Language == "en");
        }

        static void Add(string path, int priority, Action execute, Func<bool> checkedState)
        {
            Func<bool> validate = () => { Menu.SetChecked(path, checkedState()); return true; };
            AddItem.Invoke(null, new object[] { path, string.Empty, checkedState(), priority, execute, validate });
            paths.Add(path);
        }

        static void Remove()
        {
            foreach (string path in paths) RemoveItem.Invoke(null, new object[] { path });
            paths.Clear();
        }

        static MethodInfo Required(string name) => typeof(Menu).GetMethod(name, Members) ??
            throw new NotSupportedException(L10n.Tr("Unity 菜单接口不兼容：", "Unity menu API is incompatible: ") + name);
    }
}
