# 哔哩哔哩发布草稿

## 推荐标题

我给 Unity UI Builder 补了源码联动和布局诊断｜MIT 开源

备选标题：

- Unity UI Builder 看不懂 UXML/USS 对应关系？这个开源插件可以直接定位
- 从元素跳到 UXML 和 USS：我做了一个 Unity UI Builder 辅助插件
- 初学 Unity 6 UI Toolkit，我借助 GPT 给 UI Builder 补了些信息

## 视频简介

我是 **UI Builder Navigator** 的作者。这个项目起于我尝试使用 Unity 6 的 UI Toolkit 做界面。对刚开始学习的我来说，UI Builder 展示的信息偏少，操作起来也不太顺手：选中的元素对应哪段 UXML、应用了哪些 USS、样式为什么生效、父容器怎样影响布局，都需要来回查找。

于是，我开始借助 GPT 开发扩展，尝试把这些缺少的信息补到 UI Builder 里。这个插件就从学习过程中实际遇到的问题出发，逐步加上了源码联动、样式来源和布局诊断，方便我看懂正在编辑的界面，也希望能帮到遇到同样困惑的人。

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

> 尝试用 Unity 6 的 UI Toolkit 做界面时，我觉得 UI Builder 给的信息太少，用起来也不顺手。元素对应哪段代码，样式为什么生效，布局又受谁影响，都要来回找。于是我借助 GPT，做了 UI Builder Navigator，逐步补上这些信息。它能联动定位 UXML 和 USS，查看样式来源、颜色块、盒模型和父子布局，并附有中英文示例。项目采用 MIT 许可证，目前验证的是 Unity 6000.3.27f1 的 Windows Editor，仓库和安装地址在简介里。

口播必须搭配真实 `0.2.0` 操作录屏；具体镜头顺序见 `recording-script.md`。
