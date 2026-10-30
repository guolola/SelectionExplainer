# SelectionExplainer v0.2.0 Preview

## 更新重点

- Windows x64 原生划词解释原型：拖选／双击后延迟自动解释，不增加“解释”按钮。
- 兼容 Chat Completions 的模型配置、SSE 流式响应、JSON 回退和错误提示。
- 原生 WinForms 圆角卡片浮窗、固定／复制／重试、托盘暂停、应用排除及旧请求取消。
- 基础 Markdown 转原生 RTF，流式阶段保留阅读选择和滚动位置。
- 当前用户 DPAPI 密钥加密、本机设置原子保存，不保存解释历史。
- 中英双语仓库说明、原创 Logo、Apache-2.0 许可证、构建测试及校验发布流程。

## 功能边界

这是早期预览版，界面及默认回答语言为简体中文。仅支持提供 UI Automation 文本选区的应用；没有 OCR、剪贴板兜底、完整 Markdown 引擎、自动更新或开机启动注册。新版浮窗已有离屏渲染及 22 项本地自动测试，实际视觉与跨应用验收仍待完善。

项目所有者已报告原始调用链路打通；不把单机反馈当作所有软件兼容证明。自动测试使用模拟服务，真实供应商速度、费用与回答质量由实际配置决定。

## 下载与校验

发布附件包含 Windows x64 便携 ZIP、源码 ZIP、源码 tar.gz、`release.json` 和 `SHA256SUMS`。要求 Windows 10/11 x64 与 .NET Framework 4.8。预览 EXE 未代码签名。

在 PowerShell 中运行 `Get-FileHash <下载文件> -Algorithm SHA256`，与 `SHA256SUMS` 的同名条目对照。源码构建与测试步骤见 [操作手册](docs/context/RUNBOOK.md)。

## Verification

Builds and tests run before packaging. Checks cover endpoint validation, DPAPI, settings replacement, streamed Chinese text, JSON fallback, HTTP errors, cancellation and invalid selections. The artifacts exclude credentials, user settings and runtime history. Consult the workflow attached to the release commit for the actual CI outcome.

## English summary

First public Windows x64 preview of automatic highlighted-text explanations, including the 0.2 floating-card revision. It uses native WinForms/UI Automation and an installed .NET Framework runtime, with configurable model endpoints, streaming output, pin/copy/retry, basic RTF formatting, request cancellation and current-user encrypted keys. Application compatibility and visual acceptance are still being refined. OCR, clipboard fallback, a full Markdown engine, automatic updates and startup registration are not included.
