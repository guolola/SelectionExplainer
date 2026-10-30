[English](./README.md) | **简体中文**

<p align="center">
  <a href="https://github.com/guolola/SelectionExplainer">
    <img src="./docs/brand/logo.png" width="120" height="120" alt="SelectionExplainer 划词解释 Logo" />
  </a>
</p>

<div align="center">

# SelectionExplainer · 划词解释

<p>
  <strong>划选一段文字，解释就出现在旁边。</strong><br />
  使用自己的模型服务，在 Windows 中自动解释选中文字的轻量桌面助手。<br />
  <sub>选中即解释 · 流式输出 · 原生桌面浮窗</sub>
</p>

<p>
  <a href="https://github.com/guolola/SelectionExplainer/releases"><img src="https://img.shields.io/badge/下载-Windows%20x64-2563eb?style=for-the-badge" alt="下载 Windows 版" /></a>
  <a href="./docs/guide.md"><img src="https://img.shields.io/badge/使用指南-在线阅读-0f766e?style=for-the-badge" alt="使用指南" /></a>
  <a href="./LICENSE"><img src="https://img.shields.io/badge/License-Apache--2.0-7c3aed?style=for-the-badge" alt="Apache-2.0 许可证" /></a>
</p>

<p>
  <img src="https://img.shields.io/badge/Version-v0.2.0%20preview-2563eb?style=flat-square" alt="v0.2.0 预览版" />
  <img src="https://img.shields.io/badge/Platform-Windows%20x64-0078d4?style=flat-square" alt="Windows x64" />
  <img src="https://img.shields.io/badge/UI-WinForms-512bd4?style=flat-square" alt="原生 WinForms 界面" />
  <img src="https://img.shields.io/badge/Runtime-.NET%20Framework%204.8-512bd4?style=flat-square" alt=".NET Framework 4.8" />
  <a href="https://github.com/guolola/SelectionExplainer/actions/workflows/ci.yml"><img src="https://github.com/guolola/SelectionExplainer/actions/workflows/ci.yml/badge.svg" alt="构建与测试" /></a>
</p>

> 阅读时不用来回切换：选中句子、术语或代码片段，让小浮窗直接解释。

</div>

## 项目介绍

**鼠标选中文字、松开后，自动生成解释。** 无需再点“解释”按钮，也不要求按快捷键。程序通过 Windows UI Automation 获取支持应用中的选区，等待选区稳定后，在鼠标附近流式显示模型回答。

当前桌面界面及默认解释指令为简体中文，仓库提供中英双语说明。这是早期预览版，应用兼容性及界面设计仍在完善。

```text
鼠标选中文字 → 松开鼠标 → 在旁边阅读解释
                           ↳ 重新选中文字会取消旧请求
```

## 核心能力

| 功能 | 当前范围 |
| --- | --- |
| 选中即解释 | 在提供 UI Automation 文本选区的应用中，拖选或双击取词后自动触发 |
| 流式输出 | 兼容 Chat Completions SSE 流式响应，也支持 JSON 响应回退 |
| 自带模型服务 | 可配置基础地址、模型、API Key 与解释指令 |
| 原生浮窗 | 圆角卡片、原文引用区、固定／关闭、复制／重试／设置，随内容调整高度 |
| 阅读排版 | 流式结束后通过原生 RTF 排版标题、粗体、列表和代码 |
| 暂停与应用排除 | 托盘暂停／恢复，可配置排除的进程名称 |
| 取消旧请求 | 重新划词、关闭浮窗时取消此前请求 |
| 本机密钥保护 | 使用当前 Windows 用户的 DPAPI 加密，设置文件不保存明文 Key |
| 轻量分发 | 单 EXE，使用系统已有 .NET Framework，不内嵌 Electron、WebView 或模型 |

### 当前边界

- 不支持扫描 PDF、图片、游戏画面或不开放选区的控件；当前没有 OCR 或剪贴板兜底。
- 只发送选中文字，不主动获取前后段落。会跳过密码控件及被排除进程，但普通选中文字仍可能包含敏感内容。
- 默认延迟 450 毫秒，选区上限 4,000 字符，可在设置中调整；请求超时为 90 秒。
- 不保存解释历史，不自动注册开机启动；托盘暂停仅影响当前会话。基础 Markdown 排版不等于完整 Markdown 引擎，不执行 HTML。
- 模型质量、速度、可用性和费用由服务商决定。API Key 与聊天产品订阅分别配置。

## 界面预览

![带原文引用、解释正文和复制重试操作的原生浮窗](docs/images/popup-preview.png)

这是实际 WinForms 控件使用预设样本文字的离屏渲染，不是桌面截图、真实模型回答或响应时间测量。本预览包含首轮浮窗改版，跨应用和视觉验收仍待完善。交互约定及参考来源见 [浮窗设计说明](docs/context/UI.md)。

## 快速开始

### 1. 下载运行

从 [Releases](https://github.com/guolola/SelectionExplainer/releases) 下载 Windows x64 压缩包，解压后运行 `SelectionExplainer.exe`。要求 Windows 10/11 x64 与 .NET Framework 4.8，无需安装器或管理员权限。

预览版暂未代码签名，请遵循系统正常的软件审查流程，不需要关闭安全防护。

### 2. 配置模型

首次运行填写 API 基础地址、模型名称和 Key。例如基础地址为 `https://api.deepseek.com/v1`，程序自行追加 `/chat/completions`。兼容的本机回环接口可使用 HTTP 并留空 Key。

设置页会说明选中文字将发送到你配置的服务。[使用指南](docs/guide.md) 包含配置、隐私和故障排查。

### 3. 开始划词

在支持的应用中拖选一小段文字，松开鼠标后等待浮窗显示。双击托盘图标修改设置，右键菜单可暂停或退出。

## 从源码构建

在 Windows x64 上使用 Windows PowerShell 5.1 或 PowerShell 7，并准备 .NET Framework 4.8 及其框架编译器。无需还原第三方包。

```powershell
git clone https://github.com/guolola/SelectionExplainer.git
cd SelectionExplainer
.\build.ps1 -Test
.\dist\SelectionExplainer.exe
```

构建使用系统 `csc.exe`、框架程序集、应用清单及项目图标。具体操作见 [构建与发布手册](docs/context/RUNBOOK.md)。

## 项目结构

| 位置 | 用途 |
| --- | --- |
| `src/` | 桌面界面、选区获取、模型请求及配置 |
| `tests/` | 本机 HTTP 模拟、配置及请求验证 |
| `docs/brand/` | 原创 Logo、程序图标及素材来源 |
| `docs/context/` | 当前状态、决定、风险及按需上下文 |
| `.github/workflows/` | Windows 构建测试与标签预览发布 |
| `scripts/package-release.ps1` | 便携包、源码归档、元数据与校验文件 |

## 验证情况

自动测试覆盖地址校验、DPAPI、设置原子替换、中文流式拼接、JSON 回退、认证错误、取消、空响应和选区超长，也覆盖原生富文本、Unicode／RTF 转义、阅读选区保留、固定和重试事件。使用合成样本文字与本机模拟服务，不消耗付费模型额度。

项目所有者已反馈原始“划词到解释”链路能够打通；这不代表所有软件兼容。实际测试结果、资源测量范围和待完成验证统一记录在 [当前状态](docs/context/NOW.md)。

## 版本与后续计划

[版本说明](release-notes.md) 记录预览范围与验证限制。标签发布包含 Windows 便携 ZIP、源码 ZIP／tar.gz、`release.json` 与 `SHA256SUMS`。

后续重点是验收改版浮窗、扩展应用兼容性并进行更长时间的资源测量；计划不代表当前已实现。

## 参与贡献

请先看 [贡献指南](contributing.md)，通过 [项目索引](PROJECT_INDEX.md) 按任务定位上下文。反馈问题时使用仓库模板，不附带密钥或私人选中文字。

敏感问题按 [安全说明](security.md) 处理。项目使用 [Apache-2.0](LICENSE)，归属和素材说明见 [NOTICE](NOTICE)。
