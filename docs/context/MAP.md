# 项目位置映射

## 目录关系

项目根目录为本文件所在目录的 `../..`，发布目录名为 `SelectionExplainer`。以下路径均相对于项目根目录，不记录个人设备绝对路径。

| 位置 | 职责与关系 |
| --- | --- |
| `AGENTS.md` | 根级协作规则，约束本项目及子目录 |
| `PROJECT_INDEX.md` | 任务路由入口，链接根文档与专题 |
| `README.md` | 项目用途及基本使用入口 |
| `docs/context/` | 当前状态、位置映射、操作方法、决定及风险专题 |
| `docs/context/history/` | 必要的历史归档及其索引；不承载当前状态 |
| `src/`、`tests/` | 原生应用源码及本机模拟服务测试 |
| `docs/brand/` | 生成的 Logo、程序 ICO、素材来源 |
| `docs/guide.md`、`docs/release/` | 使用指南与便携包附带说明 |
| `build.ps1`、`scripts/package-release.ps1` | 编译测试及发布打包 |
| `.github/workflows/` | Windows CI 及标签预览发布 |
| `dist/`、`work/`、`artifacts/` | 忽略的本机编译、验证与发布产物；不进入源码提交 |

具体任务到专题文件的对应关系只在 [项目索引](../../PROJECT_INDEX.md) 维护。

## 版本与运行位置

核验日期：2026-10-30。范围：独立发布快照的代码、文档与打包路径。

- 产品版本：`src/AssemblyInfo.cs`，当前 `0.2.0.0`，对应 `v0.2.0` 预览发布。打包脚本校验版本匹配。
- 运行配置：`%LOCALAPPDATA%\SelectionExplainer\settings.json`；也可用 `--data-dir` 指定验证目录。不得将设置或密钥纳入仓库。
- 分发形式：单 EXE 的 Windows x64 便携 ZIP，依赖已安装 .NET Framework 4.8，无固定安装目录；附件位于 `artifacts/`。
- 远程目标：`https://github.com/guolola/SelectionExplainer`；目标约定不等于远程创建或推送已成功，结果见 [NOW.md](NOW.md)。
- 技术栈与目标平台决定：见 [DECISIONS.md](DECISIONS.md)；不根据目录名推断环境。
