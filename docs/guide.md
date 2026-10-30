# 使用指南

## 首次运行

1. 在 Windows 10/11 x64 中解压发布包，运行 `SelectionExplainer.exe`。需要已安装 .NET Framework 4.8。
2. 在设置窗口填写 API 基础地址、模型名称和 API Key；本机兼容模型接口可不填 Key。
3. 保存设置，在支持选区的应用中鼠标拖选一句话或双击一个词。松开鼠标后等待自动解释，无需额外点击。

基础地址不是完整补全接口；例如 `https://api.deepseek.com/v1`，不要填写 `/chat/completions` 后缀或将密钥放进网址。非本机服务要求 HTTPS；HTTP 仅允许回环地址。

界面及默认解释语言为中文；可通过解释指令改变回答方式。流式结束后支持标题、粗体、列表和代码的基础 Markdown 排版，使用原生 RTF，不是完整 Markdown 引擎。

## 浮窗与托盘

- 浮窗靠近选区结束位置出现，不主动抢占焦点；可选择文字，也可点底部复制按钮复制可读正文。
- 标题区可拖动，右下角可调节大小；默认随内容增长，长内容可滚动。顶部图钉可固定浮窗，固定时普通外部点击不收起，新选区仍更新回答且保持位置。
- 底部“重试”会重新请求当前选区，“设置”打开模型设置。
- 新一次鼠标选择会取消旧请求。未固定时点击窗外，或关闭浮窗，也会停止该次解释。
- 双击托盘图标打开设置；右键可暂停／恢复自动解释或退出。
- 暂停只在本次运行生效；要持久关闭自动解释，在设置中取消启用后保存。
- 若程序提示已在运行，请从系统托盘打开已有实例。

## 设置与数据

| 选项 | 当前行为 |
| --- | --- |
| 触发延迟 | 默认 450 ms，可设 200–3,000 ms |
| 文本上限 | 默认 4,000 字符，可设 50–20,000；超过上限会提示，不静默截断后发送 |
| 排除的进程名 | 英文逗号分隔，如 `KeePass,1Password,Bitwarden`；填写进程名而非窗口标题 |
| 解释指令 | 发送给模型的系统指令，可调整术语、代码解释及语言风格 |
| API Key | 使用 Windows 当前用户 DPAPI 加密后保存 |

配置保存在 `%LOCALAPPDATA%\SelectionExplainer\settings.json`。它包含 API 地址、模型和其他设置，但 Key 字段为密文。密钥与当前 Windows 用户绑定，不应将配置复制给其他用户。使用 `--data-dir <目录>` 可指定独立配置位置，用于本地验证。

只有选中的文本会发给你配置的模型；不会主动上传前后段落，不写解释历史。模型服务商仍按其政策处理收到的内容并计费。可通过暂停或应用排除减少不希望的触发；不应将密码控件识别视为完整的数据防泄漏保障。

## 没有浮窗

1. 确认托盘处于启用状态，目标应用没有被排除。
2. 先用少量普通文本拖选测试，不要使用截图、扫描文档或游戏画面。
3. 部分控件不提供 UI Automation 选区；当前版本不会模拟复制或扫描整个页面。需要在支持的应用中阅读。
4. 如果目标应用以管理员权限运行，而本程序没有相同权限，选区可能不可读；优先在普通权限应用验证，不要求为此提升整个工具权限。

## 浮窗有错误或没有解释

- 认证失败：核对 Key、基础地址和服务权限；API 权限与聊天产品订阅不同。
- 请求受限／额度不足：检查服务商配额或稍后再试。
- 无法连接：检查地址、网络与当前系统代理。
- 没有解释内容：确认服务支持兼容 Chat Completions 的文本响应。
- 请求超过 90 秒：检查服务延迟或换用其他可用模型。

不要在公开 Issue 中贴真实密钥、含秘密的请求、完整设置文件或私人阅读内容。使用合成样本复现问题。

## English quick reference

Run the executable, configure the API base URL, model and key, then select text in a UI Automation-compatible application. Double-click the tray icon for settings; its context menu pauses or exits. Only the selected text is sent. Keys use current-user Windows DPAPI; no explanation history is saved. Unsupported selection providers, scanned content and OCR are outside this preview's scope.
