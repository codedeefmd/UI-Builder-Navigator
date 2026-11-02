# Promotion Materials / 推广材料

本目录保存 UI Builder Navigator `0.2.0` 的发布说明、社区文案和演示素材。**GitHub Release 已正式发布；社区提交结果见下方状态记录。**

## 文件用途

| 文件 | 用途 |
| --- | --- |
| `unity-discussions.en.md` | Unity Discussions 的英文标题和正文草稿 |
| `reddit.en.md` | Reddit `r/Unity3D` 的英文标题、正文和发帖前检查项 |
| `reddit-moderator-review.en.md` | Reddit 账号 Karma 门槛触发后的人工复核请求正文与发送记录 |
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

## 2026-11-02 发布状态

- GitHub 仓库简介与 8 个 Topics 已更新。
- GitHub [`v0.2.0` Release](https://github.com/codedeefmd/UI-Builder-Navigator/releases/tag/v0.2.0) 已于北京时间 2026-11-02 10:04 正式发布，附带安装包和中英文实机演示。安装包已通过匿名下载与 SHA-256 一致性检查。
- `media/social-preview-en.png` 已准备，但当前自动化浏览器未登录 GitHub，尚未设置仓库的 Social preview。登录后可从 **Settings → Social preview** 上传该 PNG。
- Unity Discussions：已于 2026-11-02 提交到 UI Toolkit 分类，附英文实机截图和公开演示链接，标签为 `Conversation`、`6.3`。页面返回 **Post Needs Approval**，并在作者的 [Pending 记录](https://discussions.unity.com/u/poorlittleape/activity/pending) 中显示该标题，当前等待版主审核，尚无公开主题链接；待审记录需要登录作者账号查看。
- Reddit：已于 2026-11-02 提交 [介绍帖](https://www.reddit.com/r/Unity3D/comments/1xllk46/i_built_an_mitlicensed_ui_builder_extension_for/)，使用 `Resources/Tutorial` 分类，附英文实机视频。匿名复核发现帖子被 AutoModerator 自动移除；机器人说明当前账号需达到总 Karma 至少 50，或 `r/Unity3D` 社区 Karma 至少 20。当前未正常公开，保留原帖，不重复发布。经作者明确授权后，已于同日向 `r/Unity3D` 版主发送一次人工复核请求，并在 Reddit 会话中确认收件方、标题及正文，等待回复；请求全文见 `reddit-moderator-review.en.md`。
- 中英文实机短片、封面及哔哩哔哩文稿均已准备好。

## 统一事实边界

- 仓库：<https://github.com/codedeefmd/UI-Builder-Navigator>
- Git UPM：`https://github.com/codedeefmd/UI-Builder-Navigator.git?path=/Packages/com.poorl.ui-builder-navigator`
- 包名与版本：`com.poorl.ui-builder-navigator` `0.2.0`
- 许可证：MIT
- 已验证环境：Unity `6000.3.27f1`，Windows Editor
- 插件使用 Unity UI Builder 的内部 Editor API；其他 Unity 版本与操作系统尚未验证。
- 插件只在 Editor 中运行，不进入 Player 构建。

## 建议发布顺序

1. 已完成：正式发布 GitHub `v0.2.0` Release，并核对公开安装包。
2. 使用本目录已完成的中英文实机短片，必要时根据 `recording-script.md` 增补旁白。
3. 已提交 Unity Discussions 主题，等待审核；公开后补充主题链接。
4. Reddit 已提交但触发账号 Karma 门槛。等待人工复核或账号满足条件后再按版规处理；不要删除重发。2026-11-02 核对的公开规则仍限制同一用户或项目每 7 天最多 2 帖，发帖当天仍需复核。
5. 使用中文实机录屏发布哔哩哔哩介绍。

当前材料的版本、安装 URL 和附件均已核对；正式社区发帖时仍需查看当天版规。
