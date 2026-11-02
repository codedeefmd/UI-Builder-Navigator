# 哔哩哔哩发布草稿

## 推荐标题

我给 Unity UI Builder 补了源码联动和布局诊断｜MIT 开源

备选标题：

- Unity UI Builder 看不懂 UXML/USS 对应关系？这个开源插件可以直接定位
- 从元素跳到 UXML 和 USS：我做了一个 Unity UI Builder 辅助插件

## 视频简介

我是 **UI Builder Navigator** 的作者。学习和使用 UI Toolkit 时，我经常需要在画布元素、UXML 标签、匹配的 USS 规则和最终布局值之间来回查找，因此做了这个 Editor 扩展，把这些信息联动到 UI Builder 里。

`0.2.0` 目前包括：

- 选中画布或 Hierarchy 元素后定位 UXML 开始标签；
- 从样式列表或“相关样式”跳到对应 USS 规则，同时保留当前元素；
- UXML/USS 完整内存预览的语法配色、搜索与结果定位；
- 只读样式来源、覆盖链和透明色颜色块；
- 计算盒模型、父容器/当前元素布局对照及画布范围高亮；
- 三页中英文 Learning Lab 图文示例。

项目采用 MIT 许可证。当前已验证环境是 **Unity 6000.3.27f1、Windows Editor**；插件使用 UI Builder 内部 Editor API，其他 Unity 版本和系统尚未验证。

GitHub：<https://github.com/codedeefmd/UI-Builder-Navigator>

Package Manager 的 Git URL：

```text
https://github.com/codedeefmd/UI-Builder-Navigator.git?path=/Packages/com.poorl.ui-builder-navigator
```

安装后从 `Window → UI Toolkit → USS Preview Navigator` 打开；同一菜单的“打开示例”可进入三页 Learning Lab。

欢迎反馈你使用 UI Toolkit 时最希望补足的环节，也欢迎附上 Unity 版本、操作系统和可复现 UXML/USS 提交 Issue。

## 建议标签

`Unity` `UI Toolkit` `UI Builder` `UXML` `USS` `编辑器扩展` `开源` `游戏开发`

## 45 秒中文口播

> Unity UI Builder 能做界面，但选中一个元素后，我经常还要手动寻找它来自哪段 UXML、匹配了哪条 USS，以及父容器到底怎样影响布局。于是我做了这个开源插件 UI Builder Navigator。选中画布元素，它会定位 UXML 标签；通过“相关样式”可以跳到对应 USS，同时保留当前元素。右侧诊断页还能查看只读的样式来源、覆盖关系、颜色块、盒模型，以及父容器和当前元素的布局对照。项目采用 MIT 许可证，并附带三页中英文 Learning Lab。目前只验证了 Unity 6000.3.27f1 的 Windows Editor，仓库和安装地址在简介里，欢迎告诉我你最想继续补哪项 UI Builder 能力。

口播必须搭配真实 `0.2.0` 操作录屏；具体镜头顺序见 `recording-script.md`。
