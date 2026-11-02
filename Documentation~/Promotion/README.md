# Promotion Materials / 推广材料

本目录保存 UI Builder Navigator `0.2.0` 的对外发布草稿。**截至目前，这些文案尚未发布到 Unity Discussions、Reddit 或哔哩哔哩。**

## 文件用途

| 文件 | 用途 |
| --- | --- |
| `unity-discussions.en.md` | Unity Discussions 的英文标题和正文草稿 |
| `reddit.en.md` | Reddit `r/Unity3D` 的英文标题、正文和发帖前检查项 |
| `bilibili.zh.md` | 哔哩哔哩中文标题、简介、标签和 45 秒口播草稿 |
| `release-notes.md` | GitHub Release `v0.2.0` 中英文发布说明 |
| `recording-script.md` | 中英文同构的 30–45 秒真实操作录屏脚本 |
| [`media/social-preview-en.png`](media/social-preview-en.png) | `1280 × 640` 英文 GitHub Social preview 分享封面 |
| [`media/bilibili-cover-zh.png`](media/bilibili-cover-zh.png) | `1280 × 720` 中文哔哩哔哩封面 |
| [`media/cover.html`](media/cover.html) | 两张静态封面的可编辑源文件 |
| [`media/ui-builder-navigator-demo-en.mp4`](media/ui-builder-navigator-demo-en.mp4) | 42 秒英文实机演示，1920×1080、30fps 导出，带英文字幕 |
| [`media/ui-builder-navigator-demo-zh.mp4`](media/ui-builder-navigator-demo-zh.mp4) | 42 秒中文实机演示，1920×1080、30fps 导出，带中文字幕 |
| [`media/captions-en.srt`](media/captions-en.srt) / [`media/captions-zh.srt`](media/captions-zh.srt) | 两版成片的独立字幕源文件 |

两张 PNG 基于现有 Unity Editor 实机截图制作，用作分享卡片和视频封面。两条 MP4 则是在 Unity 6000.3.27f1 Windows Editor 中实际运行插件、自动触发原生选择/来源导航/布局悬停回调并逐帧采集窗口得到的实机演示，字幕已烧录，无旁白；临时 Editor 驱动已清除。录屏约 8fps 采集，按真实时间线导出为 30fps。

## 2026-11-02 发布准备状态

- GitHub 仓库简介与 8 个 Topics 已更新。
- GitHub [Releases 中的 `v0.2.0` 草稿](https://github.com/codedeefmd/UI-Builder-Navigator/releases) 已创建，附带安装包和中英文实机演示，尚未公开发布；登录仓库管理账号后可查看草稿。
- `media/social-preview-en.png` 已准备，但当前自动化浏览器未登录 GitHub，尚未设置仓库的 Social preview。登录后可从 **Settings → Social preview** 上传该 PNG。
- 中英文实机短片、封面、社区稿与 Release 说明已经准备好；社区帖子仍待作者审阅和发布。

## 统一事实边界

- 仓库：<https://github.com/codedeefmd/UI-Builder-Navigator>
- Git UPM：`https://github.com/codedeefmd/UI-Builder-Navigator.git?path=/Packages/com.poorl.ui-builder-navigator`
- 包名与版本：`com.poorl.ui-builder-navigator` `0.2.0`
- 许可证：MIT
- 已验证环境：Unity `6000.3.27f1`，Windows Editor
- 插件使用 Unity UI Builder 的内部 Editor API；其他 Unity 版本与操作系统尚未验证。
- 插件只在 Editor 中运行，不进入 Player 构建。

## 建议发布顺序

1. 审阅 GitHub `v0.2.0` Release 草稿及其安装包后正式发布。
2. 使用本目录已完成的中英文实机短片，必要时根据 `recording-script.md` 增补旁白。
3. 发布 Unity Discussions 主题，收集 UI Toolkit 开发者对兼容性和工作流的反馈。
4. 发帖当天重新核对 `r/Unity3D` 当前规则与同项目发帖频率，再发布 Reddit 主题。2026-10-30 查阅时的限制是同一项目 7 天内最多 2 帖；这只是历史快照，不能代替发帖当天的规则。
5. 使用中文实机录屏发布哔哩哔哩介绍。

当前材料的版本、安装 URL 和附件均已核对；正式社区发帖时仍需查看当天版规。
