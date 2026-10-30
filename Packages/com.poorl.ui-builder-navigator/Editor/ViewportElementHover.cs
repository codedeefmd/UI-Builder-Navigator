using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UIBuilderUssNavigator
{
    /// <summary>
    /// Viewport 内的临时元素检查。使用原生拾取，只更新共享调试层，不发送选择通知或操作代码预览。
    /// 鼠标移出、开始拖动、窗口失焦、进入 Preview 模式或文档替换时结束悬停。
    /// </summary>
    internal sealed class ViewportElementHover : IDisposable
    {
        readonly BuilderAccess access;
        readonly InspectorBoxModel inspector;
        readonly CanvasBoxModelOverlay overlay;
        readonly VisualElement viewport;
        VisualElement target;
        Vector2 pointerPosition;

        internal bool IsAttached => ReferenceEquals(viewport, access.Viewport) && viewport.panel != null;

        internal ViewportElementHover(BuilderAccess access, InspectorBoxModel inspector)
        {
            this.access = access;
            this.inspector = inspector;
            overlay = inspector.CanvasOverlay;
            viewport = access.Viewport;
            // 原生 OnHover 会在冒泡时停止 MouseMove，先在捕获阶段读取，随后仍让 Unity 正常处理。
            viewport.RegisterCallback<MouseMoveEvent>(OnMouseMove, TrickleDown.TrickleDown);
            viewport.RegisterCallback<MouseLeaveEvent>(OnMouseLeave);
            viewport.RegisterCallback<PointerDownEvent>(OnPointerDown, TrickleDown.TrickleDown);
        }

        void OnMouseMove(MouseMoveEvent evt)
        {
            if (evt.pressedButtons != 0 || access.IsPreviewEnabled)
            {
                Clear();
                return;
            }
            pointerPosition = evt.mousePosition;
            InspectAtPointer();
        }

        void InspectAtPointer()
        {
            if (!viewport.worldBound.Contains(pointerPosition))
            {
                Clear();
                return;
            }
            target = access.PickViewportElement(pointerPosition);
            if (target == null)
            {
                Clear();
                return;
            }
            inspector.ClearHover();
            overlay.ShowInspection(target);
        }

        void OnMouseLeave(MouseLeaveEvent evt) => Clear();
        void OnPointerDown(PointerDownEvent evt) => Clear();

        /// <summary>沿用现有刷新周期，让静止鼠标下的布局/缩放变化也能更新，不增加全局定时器。</summary>
        internal void Refresh()
        {
            if (target == null) return;
            if (target.panel != viewport.panel || access.IsPreviewEnabled || !overlay.IsInspection)
            {
                Clear();
                return;
            }
            InspectAtPointer();
        }

        internal void Clear()
        {
            target = null;
            // 右侧盒模型接管后，其逐层高亮由它自身释放，避免两个入口相互清除。
            overlay.Hide(CanvasOverlayOwner.ViewportInspection);
        }

        public void Dispose()
        {
            Clear();
            viewport.UnregisterCallback<MouseMoveEvent>(OnMouseMove, TrickleDown.TrickleDown);
            viewport.UnregisterCallback<MouseLeaveEvent>(OnMouseLeave);
            viewport.UnregisterCallback<PointerDownEvent>(OnPointerDown, TrickleDown.TrickleDown);
        }
    }
}
