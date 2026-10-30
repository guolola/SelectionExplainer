# 浮窗设计与参考

## 参考来源

核验日期：2026-10-30；范围仅为相关浮窗组件与窗口行为，不是这些仓库的全量审查。参考交互模式，当前 WinForms 实现为本项目编写，未引入这些项目的依赖或复制其代码。

| 来源 | 核对到的做法 | 本项目应用 |
| --- | --- | --- |
| [Cherry Studio 工具条](https://github.com/CherryHQ/cherry-studio/blob/f97518aaed08440e75c31639d02f0c7870685029/src/renderer/components/selection/SelectionToolbarView.tsx) 与 [结果窗口](https://github.com/CherryHQ/cherry-studio/blob/f97518aaed08440e75c31639d02f0c7870685029/src/renderer/windows/selection/action/ActionWindow.tsx) | 紧凑圆角、细边界、图标交互反馈、拖动区、固定按钮 | 自绘轻量按钮、拖动标题区、复制反馈；保持自动解释 |
| [Pot 翻译窗口](https://github.com/pot-app/pot-desktop/blob/master/src/window/Translate/index.jsx) | SourceArea 与 TargetArea 分区、固定/关闭按钮、可滚动内容 | 淡色原文引用区与独立解释正文，长内容可滚动 |
| [Easydict 窗口管理](https://github.com/tisfeng/Easydict/blob/be3b7a1eafb9092acedde3a727b2b87edb76981d/Easydict/objc/ViewController/Window/WindowManager/EZWindowManager.m) | 临时窗口与固定窗口的关闭策略不同，固定状态保留位置 | 临时浮窗外点收起，固定后保留窗口，不随新选区重新定位 |

## 当前交互约定

- 鼠标拖选或双击后自动开始解释，附近显示小卡片；出现时不主动抢占原应用焦点。
- 默认宽度 440 个基础像素，圆角 12；随内容调整高度，最高 580，之后滚动。标题可拖动，右下角可调整大小。
- 顶部是标题、固定和关闭；原文使用淡底引用区；状态放在正文之前，底部放复制、重试与设置。
- 未固定时点击外部取消当前请求并收起；固定后普通外部点击不收起，新划词仍更新解释，保持浮窗位置。
- 流式阶段增量追加并保留文本选择与滚动位置；完成后用原生 RTF 排版标题、粗体、列表、行内代码和代码块。不是完整 Markdown 引擎，不执行 HTML。
- 复制输出可读正文，反馈“已复制”；重试会重新请求当前选区；关闭或新选区使旧请求失效。

## 验证边界

预览见 [popup-preview.png](../images/popup-preview.png)，为真实控件的离屏示例渲染，正文使用同一 RichEdit 的打印接口，不能当作真实模型输出或桌面截图。实际交互与视觉验收状态见 [NOW.md](NOW.md)。多显示器和动态 DPI 尚未完整验证。
