using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace UIBuilderUssNavigator
{
    /// <summary>
    /// 为 UI Builder 添加 UXML/USS 预览配色、选择联动、Inspector 盒模型与样式来源，不打开额外窗口。
    /// 仅更新编辑器视觉树；用户执行样式导航时使用原生选择通知，不写 UXML/USS 或保存文档。
    /// 关闭菜单开关、关闭窗口或脚本重载时，移除自有控件并恢复原生代码预览。
    /// </summary>
    [InitializeOnLoad]
    internal static class UssPreviewNavigator
    {
        const string PreferenceKey = "UIBuilderUssNavigator.Enabled";
        const double PollInterval = 0.15;
        static bool enabled = EditorPrefs.GetBool(PreferenceKey, true);
        static bool suspended;
        static double nextPoll;
        static PreviewSession session;

        static UssPreviewNavigator()
        {
            NavigatorMenu.Initialize(() => SetEnabled(!enabled), () => enabled && !suspended);
            EditorApplication.update += Update;
            AssemblyReloadEvents.beforeAssemblyReload += Release;
            EditorApplication.quitting += Release;
        }

        /// <summary>用户开关保存在 EditorPrefs；重新打开时也允许重试一次已明确报错的接口绑定。</summary>
        internal static void SetEnabled(bool value)
        {
            enabled = value;
            suspended = false;
            EditorPrefs.SetBool(PreferenceKey, value);
            Release();
            nextPoll = 0;
        }

        /// <summary>
        /// UI Builder 的选择通知接口是 internal，外部插件不能直接实现它。
        /// 用低频轮询读取一次当前选择；选择和预览文本未变化时，不解析、不刷新列表或改变滚动位置。
        /// </summary>
        static void Update()
        {
            if (!enabled || suspended || EditorApplication.isCompiling || EditorApplication.isUpdating ||
                EditorApplication.timeSinceStartup < nextPoll) return;
            nextPoll = EditorApplication.timeSinceStartup + PollInterval;

            try
            {
                if (session != null && (!session.Window || !session.IsAttached)) Release();
                if (session == null)
                {
                    EditorWindow window = Resources.FindObjectsOfTypeAll<EditorWindow>()
                        .FirstOrDefault(candidate => candidate.GetType().FullName == "Unity.UI.Builder.Builder");
                    if (!window) return;
                    var access = new BuilderAccess(window);
                    // 窗口构建或重载过程中预览尚未挂到面板，等正常生命周期完成后再接入。
                    if (access.Preview == null || access.Preview.panel == null ||
                        access.UxmlPreview == null || access.UxmlPreview.panel == null) return;
                    session = new PreviewSession(window, access);
                }
                session.Refresh();
                session.UpdateLayout();
            }
            catch (Exception exception)
            {
                // 扩展边界的失败必须可见：停止本次联动并恢复原生界面，避免每次 Editor tick 重复报错。
                suspended = true;
                Release();
                Debug.LogError(L10n.Format(
                    "[USS Preview Navigator] Unity {0} 联动已停止；原生预览已恢复。可通过“启用”菜单重新开启。\n{1}",
                    "[USS Preview Navigator] Integration stopped in Unity {0}; native previews were restored. Use Enabled to retry.\n{1}",
                    Application.unityVersion, exception));
            }
        }

        static void Release()
        {
            session?.Dispose();
            session = null;
        }

        /// <summary>
        /// 一次 UI Builder 窗口生命周期内的预览视图和缓存。
        /// 按行虚拟化显示完整导出文本，避免原生大 TextField 的字符数截断限制；同时只创建可见行控件。
        /// </summary>
        sealed class PreviewSession : IDisposable
        {
            readonly BuilderAccess access;
            readonly SyntaxCodePreview uss;
            readonly SyntaxCodePreview uxml;
            readonly RelatedStylesMenu relatedStyles;
            readonly InspectorDiagnosticsTabs inspectorTabs;
            readonly InspectorBoxModel boxModel;
            readonly ViewportElementHover viewportHover;
            readonly InspectorStyleSources styleSources;
            VisualElement selectedElement;
            VisualElement inspectedElement;
            VisualElement relatedElement;
            string relatedSelector;
            int relatedOccurrence;
            string selector;
            bool revealUssRequested;
            bool revealUxmlRequested;
            bool hadFocus;
            bool diagnosticsNeedRefresh = true;
            int languageRevision = -1;

            internal EditorWindow Window { get; }
            internal bool IsAttached => uss.IsAttachedTo(access.Preview) && uxml.IsAttachedTo(access.UxmlPreview) &&
                inspectorTabs.IsAttached && boxModel.IsAttached && viewportHover.IsAttached && styleSources.IsAttached;

            internal PreviewSession(EditorWindow window, BuilderAccess access)
            {
                Window = window;
                this.access = access;
                uss = new SyntaxCodePreview(access.Preview, "uss-navigator", UssSyntaxHighlighter.HighlightLines,
                    () => L10n.Tr("复制定位规则", "Copy located rule"));
                try
                {
                    uxml = new SyntaxCodePreview(access.UxmlPreview, "uxml-navigator", UxmlSyntaxHighlighter.HighlightLines,
                        () => L10n.Tr("复制定位标签", "Copy located tag"));
                    relatedStyles = new RelatedStylesMenu(window, access, (element, style) =>
                    {
                        relatedElement = element;
                        inspectedElement = element;
                        // 保存规则文本与同名序号，不持有资源刷新时会重建的选择器视觉节点。
                        relatedSelector = access.GetSelector(style);
                        relatedOccurrence = access.GetOccurrence(style, relatedSelector);
                        revealUssRequested = true;
                        Refresh();
                        UpdateLayout();
                    });
                    inspectorTabs = new InspectorDiagnosticsTabs(access.Inspector);
                    boxModel = new InspectorBoxModel(inspectorTabs.Content, access.Viewport);
                    viewportHover = new ViewportElementHover(access, boxModel);
                    styleSources = new InspectorStyleSources(inspectorTabs.Content, window.GetType().Assembly, relatedStyles);
                    inspectorTabs.PageChanged += OnInspectorPageChanged;
                }
                catch
                {
                    uss.Dispose();
                    uxml?.Dispose();
                    relatedStyles?.Dispose();
                    viewportHover?.Dispose();
                    styleSources?.Dispose();
                    boxModel?.Dispose();
                    inspectorTabs?.Dispose();
                    throw;
                }
                // USS 的临时提示可取消；UXML 表示仍在编辑的元素，保持到元素选择改变。
                Window.rootVisualElement.RegisterCallback<PointerDownEvent>(OnPointerDown, TrickleDown.TrickleDown);
            }

            internal void Refresh()
            {
                bool languageChanged = languageRevision != L10n.Revision;
                if (languageChanged)
                {
                    languageRevision = L10n.Revision;
                    // 原位换字，不重建会话、虚拟行或原生属性页，保留焦点、手动选区及浏览位置。
                    inspectorTabs.RefreshLanguage();
                    uss.RefreshLanguage();
                    uxml.RefreshLanguage();
                    boxModel.RefreshLanguage();
                    styleSources.RefreshLanguage();
                    diagnosticsNeedRefresh = true;
                }
                bool focused = UnityEditorInternal.InternalEditorUtility.isApplicationActive &&
                    EditorWindow.focusedWindow == Window;
                if (hadFocus && !focused)
                {
                    uss.ClearLocation();
                    boxModel.ClearHover();
                    viewportHover.Clear();
                }
                hadFocus = focused;
                string uxmlText = access.FullUxmlText;
                bool uxmlChanged = uxml.SetText(uxmlText);
                string text = access.FullPreviewText;
                bool textChanged = uss.SetText(text);
                VisualElement selected = access.SelectedElement;
                string nativeSelector = access.GetSelector(selected);
                // 样式规则的选择不覆盖页面元素上下文；只有改选页面元素或取消选择才更新它。
                if (selected == null) inspectedElement = null;
                else if (nativeSelector == null && access.GetUxmlElementPath(selected, out _) != null)
                    inspectedElement = selected;
                // 布局编辑不一定改变选择或预览文本；在提前返回前刷新四边和尺寸。
                // 查看相关样式时沿用 UXML 的页面元素上下文，避免把编辑器选择器行当成被检查的盒子。
                boxModel.Refresh(inspectedElement, access.IsPreviewEnabled || !inspectorTabs.DiagnosticsVisible);
                // 属性页不查询隐藏的来源链。回到诊断页时强制读一次，涵盖期间发生的未保存编辑。
                if (inspectorTabs.DiagnosticsVisible)
                {
                    styleSources.Refresh(inspectedElement, uxmlChanged || textChanged || diagnosticsNeedRefresh);
                    diagnosticsNeedRefresh = false;
                }
                else diagnosticsNeedRefresh = true;
                viewportHover.Refresh();
                if (!ReferenceEquals(inspectedElement, relatedElement) || nativeSelector != null)
                {
                    relatedElement = null;
                    relatedSelector = null;
                    relatedStyles.ClearRuleMarker();
                }
                string selectedSelector = relatedSelector ?? nativeSelector;
                bool selectionChanged = !ReferenceEquals(selectedElement, selected) || selector != selectedSelector;
                if (!textChanged && !uxmlChanged && !selectionChanged && !languageChanged &&
                    !revealUssRequested && !revealUxmlRequested) return;
                selectedElement = selected;
                selector = selectedSelector;
                RuleRange range = default;
                bool found = selector != null && UssRuleLocator.TryFind(text, selector,
                    relatedSelector != null ? relatedOccurrence : access.GetOccurrence(selected, selector), out range);
                uss.SetLocation(found, range, selectionChanged || revealUssRequested);
                uss.SetStatus(found ? $"L{range.StartLine + 1}–{range.EndLine + 1}  {selector}"
                    : selector == null ? "选择 USS 选择器以定位规则" : $"当前预览中未找到规则：{selector}",
                    found ? $"L{range.StartLine + 1}–{range.EndLine + 1}  {selector}"
                    : selector == null ? "Select a USS selector to locate its rule" : $"Rule not found in the current preview: {selector}");
                string tag = null;
                // 左右预览各自有定位目标：选中的元素与正在查看的相关规则可以同时存在。
                var path = access.GetUxmlElementPath(inspectedElement, out tag);
                RuleRange tagRange = default;
                bool tagFound = path != null && UxmlElementLocator.TryFind(uxmlText, path, out tagRange);
                uxml.SetLocation(tagFound, tagRange, selectionChanged || revealUxmlRequested);
                uxml.SetStatus(tagFound ? $"L{tagRange.StartLine + 1}–{tagRange.EndLine + 1}  <{tag}>"
                    : "选择 Hierarchy 或画布元素以定位 UXML 标签",
                    tagFound ? $"L{tagRange.StartLine + 1}–{tagRange.EndLine + 1}  <{tag}>"
                    : "Select a Hierarchy or canvas element to locate its UXML tag");
                revealUssRequested = revealUxmlRequested = false;
            }

            void OnPointerDown(PointerDownEvent evt)
            {
                uss.ClearLocation();
                if (evt.button != 0) return;
                // 同一条样式再次点击也算新的定位请求；代码区/空白区点击仅取消提示。
                for (var element = evt.target as VisualElement; element != null; element = element.parent)
                {
                    if (access.IsSelectorItem(element))
                    {
                        revealUssRequested = true;
                        break;
                    }
                    if (access.IsUxmlElementItem(element))
                    {
                        revealUxmlRequested = true;
                        break;
                    }
                }
            }

            void OnInspectorPageChanged()
            {
                boxModel.ClearHover();
                viewportHover.Clear();
                diagnosticsNeedRefresh = true;
                Refresh();
                Window.Repaint();
            }

            internal void UpdateLayout()
            {
                uss.UpdateLayout();
                uxml.UpdateLayout();
                relatedStyles.UpdateLayout();
            }

            public void Dispose()
            {
                Window.rootVisualElement.UnregisterCallback<PointerDownEvent>(OnPointerDown, TrickleDown.TrickleDown);
                relatedStyles.Dispose();
                viewportHover.Dispose();
                styleSources.Dispose();
                boxModel.Dispose();
                inspectorTabs.PageChanged -= OnInspectorPageChanged;
                inspectorTabs.Dispose();
                uss.Dispose();
                uxml.Dispose();
                Window.Repaint();
            }
        }
    }
}
