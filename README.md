# DeepSeek QQ Bot

基于 [Lagrange.Core](https://github.com/LagrangeDev/Lagrange.Core) 实现的接入 [DeepSeek](https://www.deepseek.com/) API 的 QQ 群聊机器人。

---

## ✨ 特性

- **智能上下文管理**
  - 支持多轮对话记忆（自动清理历史）
  - 支持 `@机器人 reset` 重置会话
  - 自动处理长上下文（截断优化）

- **稳定可靠**
  - API 异常自动重试（最多 100 次/间隔 60 秒，可配置）
  - 长消息智能分段发送
  - 服务恢复自动通知

- **灵活配置**
  - 支持群组白名单

---

## 🚀 开发指南

### 环境要求
- .NET 9.0 SDK 或更高版本
- Visual Studio 2022 以上，直接打开 `QQBotCSharp.sln` 编译

### 配置文件

```json
{
  "botUin": "机器人QQ号（必须）",
  "apiKey": "DeepSeek API密钥（必须）",
  "allowedGroupIds": [123456] // 允许的群号列表
}
```
