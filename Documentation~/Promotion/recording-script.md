# 30–45 second recording script / 30–45 秒录屏脚本

本脚本用于中英文同构的实机演示。2026-11-02 已完成两条约 42 秒的字幕版，见 [中文 MP4](media/ui-builder-navigator-demo-zh.mp4) / [English MP4](media/ui-builder-navigator-demo-en.mp4)。两版使用对应的页面、插件界面与字幕；目前无旁白，后面的口播稿供后续配音使用。

成片通过临时 Editor 驱动触发真实选择、规则导航、来源按钮和布局悬停回调，并约 8fps 逐帧采集实际窗口，按真实时间线导出为 1920×1080、30fps。临时驱动已从工程移除，未保存场景或 UXML/USS。

## 录制准备 / Recording setup

- 使用 Unity `6000.3.27f1` Windows Editor 和插件 `0.2.0`。
- 从 `Window → UI Toolkit → USS Preview Navigator → Open Examples / 打开示例` 打开对应语言的 Learning Lab 页面。
- 中文使用 `.zh.uxml`，英文使用 `.en.uxml`；录制前确认插件语言与页面语言一致。
- 画面只保留 UI Builder、Hierarchy、StyleSheets、Viewport、UXML/USS Preview 和 Inspector；隐藏通知、账号信息与无关窗口。
- 每一步都实际点击并等待定位或高亮出现，不用静态 mock-up 冒充功能效果。
- 建议成片 1920×1080、30 fps，时长 38–43 秒；鼠标移动要慢于平时操作，便于观看定位变化。

## 同构镜头表 / Mirrored shot list

| 时间 | 真实操作镜头 | 中文字幕 | English caption |
| --- | --- | --- | --- |
| 0–3s | 显示 UI Builder 与已打开的 Learning Lab 第 1 页。 | UI Builder Navigator · MIT 开源 | UI Builder Navigator · MIT licensed |
| 3–10s | 选中主按钮，展示画布选择与 UXML 开始标签定位。 | 选中元素 → 定位 UXML 标签 | Select an element → locate its UXML tag |
| 10–17s | 导航到实际匹配的相关 USS 规则，展示样式列表与代码定位，保留当前元素。 | 跳转相关 USS 规则，同时保留当前元素 | Follow a related USS rule, keeping the same element |
| 17–22s | 打开第 2 页并选择继承父级颜色的 Label，展示诊断页的只读来源。 | 查看最终数值与继承的样式来源 | Read computed values and inherited style sources |
| 22–28s | 触发真实颜色来源按钮，定位声明该属性的父容器规则。 | 点击来源 → 找到真正生效的声明 | Click the source to find the winning declaration |
| 28–33s | 打开第 3 页，选中百分比宽度子元素并展开父子布局对照。 | 对照父容器和当前元素的布局 | Compare the parent and current element layout |
| 33–40s | 触发布局表的真实悬停回调，显示父内容区与当前外框。 | 查看紫色父内容区与蓝色当前外框 | Inspect parent content and current border bounds |
| 40–42s | 保留实机画面，字幕显示仓库地址。 | github.com/codedeefmd/UI-Builder-Navigator | github.com/codedeefmd/UI-Builder-Navigator |

## 中文口播（约 42 秒）

> 尝试用 Unity 6 的 UI Toolkit 做界面时，我觉得 UI Builder 给的信息太少，用起来也不顺手。于是我借助 GPT，开发了 UI Builder Navigator，补充学习时想看却不容易找到的信息。选中元素能定位 UXML，通过相关样式能跳到 USS；诊断页能查看样式来源、颜色块、盒模型和父子布局。项目采用 MIT 许可证，附有中英文示例。目前验证的是 Unity 6000.3.27f1 的 Windows Editor，仓库和安装地址见说明。

## English voice-over (about 42 seconds)

> While trying UI Toolkit in Unity 6, I found UI Builder hard to follow and felt it showed too little information. I built UI Builder Navigator with GPT's help to fill the gaps I encountered while learning. Selecting an element reveals its UXML tag; Related Styles locates matching USS rules. Diagnostics shows style sources, color swatches, a box model, and parent/current layout comparisons. It is MIT licensed, with bilingual examples. Currently verified with Unity 6000.3.27f1 on Windows Editor; the repository and install URL are in the description.

## 录制后检查 / Post-recording checks

- UXML 与 USS 高亮都由真实选择或菜单操作触发，且画面能看清目标位置。
- “相关样式”跳转后，原元素仍保持选择；不要剪掉这一验证时刻。
- 父范围与当前元素范围使用实际紫色/蓝色覆盖层，不用后期绘制替代。
- 中英文版本只替换语言，不改变镜头顺序或扩大功能描述。
- 结尾兼容性文字必须保留 `6000.3.27f1` 与 `Windows Editor`，不写“支持 Unity 6 全版本”。
