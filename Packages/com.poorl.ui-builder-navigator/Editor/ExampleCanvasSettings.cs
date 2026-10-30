using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace UIBuilderUssNavigator
{
    /// <summary>
    /// 只为首次打开的示例设置 1280×900 画布，避免 UI Builder 默认 350×450 把教学页压成细长列。
    /// 使用 Builder 原生文档设置写入当前项目 Library；已有设置原样保留，不修改 UXML 或全局偏好。
    /// 内部 API 的版本边界集中在这里，其他示例导入/打开逻辑仍使用 Unity 公共接口。
    /// </summary>
    internal static class ExampleCanvasSettings
    {
        const BindingFlags Members = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        static readonly Type Builder = Type.GetType("Unity.UI.Builder.Builder, UnityEditor.UIBuilderModule", true);
        static readonly Type Settings = Builder.Assembly.GetType("Unity.UI.Builder.BuilderDocumentSettings", true);
        static readonly MethodInfo Save = Required(Settings.GetMethod("SaveSettingsToDisk", Members), "SaveSettingsToDisk");
        static readonly FieldInfo Width = Required(Settings.GetField("CanvasWidth", Members), "CanvasWidth");
        static readonly FieldInfo Height = Required(Settings.GetField("CanvasHeight", Members), "CanvasHeight");
        static readonly FieldInfo MatchGameView = Required(Settings.GetField("MatchGameView", Members), "MatchGameView");

        internal static bool IsFirstOpen(VisualTreeAsset asset)
        {
            string path = AssetDatabase.GetAssetPath(asset);
            string guid = AssetDatabase.AssetPathToGUID(path);
            string project = Directory.GetParent(Application.dataPath).FullName;
            string settingsFile = Path.Combine(project, "Library", "UIBuilder", "DocumentSettings", guid + ".json");
            return !File.Exists(settingsFile);
        }

        internal static void InitializeAfterOpen(VisualTreeAsset asset)
        {
            EditorWindow window = Resources.FindObjectsOfTypeAll<EditorWindow>().First(candidate => candidate.GetType() == Builder);
            object document = Builder.GetProperty("document", Members).GetValue(window);
            // OpenAsset 的返回值只代表资产处理器接管了请求；原生未保存询问可能取消切换。
            // 确认当前文档确实是本示例后才写首次设置，取消不会消耗首次打开状态。
            if (!ReferenceEquals(document.GetType().GetProperty("visualTreeAsset", Members).GetValue(document), asset)) return;
            object file = document.GetType().GetProperty("activeOpenUXMLFile", Members).GetValue(document);
            object settings = file.GetType().GetProperty("settings", Members).GetValue(file);
            Width.SetValue(settings, 1280);
            Height.SetValue(settings, 900);
            MatchGameView.SetValue(settings, false);
            object viewport = Builder.GetProperty("viewport", Members).GetValue(window);
            object canvas = viewport.GetType().GetField("m_Canvas", Members).GetValue(viewport);
            canvas.GetType().GetProperty("width", Members).SetValue(canvas, 1280f);
            canvas.GetType().GetProperty("height", Members).SetValue(canvas, 900f);
            Save.Invoke(settings, null);
            // 新窗口默认只显示代码区标题栏，教学步骤会找不到代码。仅在折叠时展开，保留已有布局。
            TwoPaneSplitView code = window.rootVisualElement.Q<TwoPaneSplitView>("code-preview-pane");
            if (code.layout.height <= 24)
            {
                window.rootVisualElement.Q<TwoPaneSplitView>("middle-column").fixedPaneInitialDimension = 260;
                code.fixedPaneInitialDimension = Math.Max(240, window.position.width / 3);
            }
            // 等文档进入布局再适配视口；用户已切换页面或关闭窗口时不改变他正在查看的内容。
            window.rootVisualElement.schedule.Execute(() =>
            {
                if (!window) return;
                object currentDocument = Builder.GetProperty("document", Members).GetValue(window);
                if (!ReferenceEquals(currentDocument.GetType().GetProperty("visualTreeAsset", Members).GetValue(currentDocument), asset)) return;
                viewport.GetType().GetMethod("FitViewport", Members, null, Type.EmptyTypes, null).Invoke(viewport, null);
                window.Repaint();
            }).StartingIn(16);
        }

        static T Required<T>(T member, string name) where T : MemberInfo => member ??
            throw new NotSupportedException(L10n.Tr("示例画布接口不兼容：", "Example canvas API is incompatible: ") + name);
    }
}
