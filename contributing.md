# Contributing / 参与贡献

Read [AGENTS.md](AGENTS.md), then use [PROJECT_INDEX.md](PROJECT_INDEX.md) for task-specific context. Do not load all documentation by default.

## Local checks

On Windows x64 with .NET Framework 4.8 and PowerShell:

```powershell
.\build.ps1 -Test
```

Tests use a loopback server and synthetic content. No provider key is required. Build output and local test configuration live in ignored directories.

For selection or floating-window changes, also verify the relevant desktop interaction with non-sensitive sample text. Separate automated test results, manual compatibility evidence and provider-quality checks in the PR description.

## Pull requests

Explain the concrete behavior being changed, why it matters and how it was checked. Preserve the lightweight native runtime approach unless a different direction has been agreed. Keep functional changes separate from unrelated repository presentation edits where possible.

Never commit API keys, per-user settings, local logs or generated history. Use the [bug template](.github/ISSUE_TEMPLATE/bug_report.yml) to report reproducible failures. Follow [security.md](security.md) for sensitive findings.

提交前在 Windows 中构建并运行相关检查。用合成文本测试划词，区分自动测试、人工兼容性验证和真实模型质量。不要提交本机配置、密钥或私人内容；按职责更新相关文档。
