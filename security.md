# Security / 安全说明

SelectionExplainer sends highlighted text to the model service configured by its user. Treat that destination as a data recipient. The application does not save explanation history; the provider may retain requests according to its own policy.

API keys are encrypted using Windows DPAPI for the current user. This protects stored keys from simple plaintext disclosure; it does not protect them against compromise of that user's session or device. Do not publish `settings.json`, even when the key is encrypted.

## Reporting

Do not disclose real credentials or sensitive selected text in public issues. If the repository's **Security → Report a vulnerability** flow is available, use it. Otherwise open a minimal issue asking the maintainer for a private reporting channel without revealing exploit details or affected private data. A private reporting channel has not yet been verified for this new repository.

If a provider key was exposed, revoke it through the provider before sharing a redacted report. The project does not ask users to disable system security protections or to paste keys in issues.

请勿在公开 Issue 贴密钥、完整设置或私人选中文字。优先使用仓库可用的私密漏洞报告入口；若没有，先通过不含敏感细节的问题请求私密联系渠道。
