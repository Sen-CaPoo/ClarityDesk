# Quick Start Guide: LINE 整合功能開發

**Version**: 1.0
**Date**: 2025-10-31
**Target Audience**: 開發者

## 概述

本指南提供 LINE 整合功能的快速上手步驟，包含環境設定、本地測試和部署準備。

## 前置需求

### 1. LINE Developers Console 設定

#### 建立 LINE Messaging API Channel

1. 登入 [LINE Developers Console](https://developers.line.biz/console/)
2. 建立新的 Provider（或使用現有）
3. 建立 Messaging API Channel：
   - Channel type: Messaging API
   - Channel name: ClarityDesk Bot（或自訂名稱）
   - Channel description: 問題回報單通知機器人
   - Category: 選擇適合的分類（例如：Business）
   - Subcategory: 選擇子分類

4. 設定 Channel：
   - **Channel Secret**: 複製並儲存（用於 Webhook 簽章驗證）
   - **Channel Access Token**: 點擊 "Issue" 產生長期 Token 並儲存

#### 設定 Webhook

1. 在 Channel 設定頁面找到 "Webhook settings"
2. Webhook URL: `https://your-domain.com/api/line/webhook`
   - 開發環境可使用 ngrok（見下方說明）
3. 啟用 "Use webhook"
4. 停用 "Auto-reply messages"（避免自動回覆干擾）
5. 停用 "Greeting messages"（可選）

#### 加入測試好友

1. 在 Channel 設定頁面找到 QR Code
2. 使用 LINE App 掃描加入官方帳號

### 2. 開發環境準備

#### 必要工具

- .NET 8.0 SDK
- SQL Server (LocalDB 或完整版)
- Visual Studio 2022 或 VS Code
- Git
- ngrok（用於本地 Webhook 測試）

#### 安裝 ngrok

```bash
# Windows (使用 Chocolatey)
choco install ngrok

# 或手動下載
# https://ngrok.com/download
```

## 環境設定

### 1. Clone 專案

```bash
git clone https://github.com/your-org/ClarityDesk.git
cd ClarityDesk
git checkout 001-line-integration
```

### 2. 設定資料庫連線

編輯 `appsettings.Development.json`：

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=ClarityDesk_Dev;Trusted_Connection=True;MultipleActiveResultSets=true"
  }
}
```

### 3. 設定 LINE Messaging API 金鑰

#### 方式一：User Secrets（推薦）

```bash
cd ClarityDesk
dotnet user-secrets init
dotnet user-secrets set "LineMessaging:ChannelAccessToken" "YOUR_CHANNEL_ACCESS_TOKEN"
dotnet user-secrets set "LineMessaging:ChannelSecret" "YOUR_CHANNEL_SECRET"
```

#### 方式二：appsettings.Development.json（僅限本地開發）

```json
{
  "LineMessaging": {
    "ChannelAccessToken": "YOUR_CHANNEL_ACCESS_TOKEN",
    "ChannelSecret": "YOUR_CHANNEL_SECRET",
    "WebhookPath": "/api/line/webhook",
    "ImageUploadPath": "wwwroot/uploads/line-images",
    "MaxImageSizeBytes": 10485760,
    "MaxImagesPerIssue": 3
  }
}
```

⚠️ **重要**: 絕對不要將金鑰提交到 Git！確保 `appsettings.Development.json` 在 `.gitignore` 中。

### 4. 執行資料庫遷移

```bash
# 還原 NuGet 套件
dotnet restore

# 套用資料庫遷移
dotnet ef database update
```

這會建立以下新資料表：

- `LineBindings` - LINE 綁定記錄
- `LinePushLogs` - LINE 推送記錄
- `LineConversationStates` - LINE 對話狀態

### 5. 建立圖片上傳目錄

```bash
# PowerShell
New-Item -ItemType Directory -Force -Path "wwwroot/uploads/line-images"

# Bash
mkdir -p wwwroot/uploads/line-images
```

## 本地開發與測試

### 1. 啟動應用程式

```bash
dotnet run
```

應用程式預設會在 `https://localhost:5191` 啟動。

### 2. 設定 ngrok 隧道

開啟新的終端視窗：

```bash
ngrok http https://localhost:5191
```

ngrok 會顯示轉發 URL，例如：

```text
Forwarding  https://abcd1234.ngrok.io -> https://localhost:5191
```

### 3. 更新 LINE Webhook URL

1. 回到 LINE Developers Console
2. 更新 Webhook URL 為：`https://abcd1234.ngrok.io/api/line/webhook`
3. 點擊 "Verify" 測試連線（應顯示 Success）

### 4. 測試 LINE 綁定流程

1. 登入本地應用程式：`https://localhost:5191`
2. 使用 LINE Login 登入（會建立測試使用者）
3. 導航至 `Account/LineBinding` 頁面
4. 點擊「綁定 LINE 官方帳號」按鈕
5. 系統會顯示綁定狀態

**Note**: 實際綁定需要在 LINE App 中與官方帳號互動，目前綁定狀態透過 LINE Login 的 `userId` 關聯。

### 5. 測試 Webhook 接收

在 LINE App 中向官方帳號發送訊息：

```text
回報問題
```

檢查應用程式日誌應顯示：

```text
[INFO] Received LINE webhook event: message (userId: U123...)
[INFO] Starting new conversation for user U123...
```

### 6. 測試推送通知

在網頁介面中新增問題回報單：

1. 導航至 `/Issues/Create`
2. 填寫問題資訊
3. 指派給已綁定 LINE 的處理人員
4. 儲存

已綁定的處理人員應在 LINE App 中收到 Flex Message 通知。

## 測試資料建立

### 使用 Seed Data

`ApplicationDbContextSeed.cs` 已包含基本種子資料（預設管理員和單位）。

### 手動建立測試綁定

使用 SQL Server Management Studio 或 Azure Data Studio：

```sql
-- 假設已有 UserId = 1 的使用者
INSERT INTO LineBindings (UserId, LineUserId, DisplayName, IsActive, BoundAt, CreatedAt, UpdatedAt)
VALUES (1, 'U1234567890abcdef1234567890abcdef', '測試使用者', 1, GETUTCDATE(), GETUTCDATE(), GETUTCDATE());
```

**Note**: `LineUserId` 應替換為實際的 LINE User ID（可從 Webhook 事件日誌取得）。

### 測試對話流程

完整對話流程測試：

1. **啟動流程**: 在 LINE 發送「回報問題」
2. **填寫標題**: 輸入「系統測試問題」
3. **填寫內容**: 輸入「測試內容描述」
4. **選擇單位**: 點擊快速選單「技術部」
5. **選擇緊急程度**: 點擊快速選單「🔴 高」
6. **填寫聯絡人**: 輸入「張三」
7. **填寫電話**: 輸入「0912345678」
8. **上傳圖片**: 傳送圖片或輸入「跳過」
9. **確認**: 點擊「✅ 確認送出」
10. **驗證**: 檢查網頁系統中是否出現新問題回報單

## 常見問題排解

### Webhook 驗證失敗

**症狀**: LINE Console 顯示 "Failed to verify webhook"

**可能原因**:

1. Webhook URL 不正確
2. 應用程式未啟動
3. ngrok 隧道中斷

**解決方式**:

- 確認 ngrok 仍在執行
- 檢查 Webhook Controller 是否正確回應
- 查看應用程式日誌是否有錯誤

### 簽章驗證錯誤

**症狀**: 日誌顯示 "Invalid LINE webhook signature"

**可能原因**:

- Channel Secret 設定錯誤
- 請求 Body 編碼問題

**解決方式**:

```csharp
// 確認 LineWebhookController 中的簽章驗證邏輯
// 使用原始請求 Body（不可先反序列化）
```

### 推送訊息失敗

**症狀**: LinePushLog 記錄顯示 Status = "Failed"

**可能原因**:

1. Channel Access Token 無效或過期
2. LINE User ID 錯誤
3. Flex Message 格式錯誤
4. 超過 Rate Limit

**解決方式**:

- 檢查 `ErrorMessage` 欄位詳細錯誤
- 使用 [Flex Message Simulator](https://developers.line.biz/flex-simulator/) 驗證格式
- 確認 Channel Access Token 未過期

### 圖片下載失敗

**症狀**: 對話流程中上傳圖片後無回應

**可能原因**:

- LINE Content API 認證失敗
- 圖片檔案過大
- 上傳目錄權限不足

**解決方式**:

```bash
# 確認目錄權限
icacls "wwwroot/uploads/line-images" /grant "IIS_IUSRS:(OI)(CI)F"

# 檢查日誌
tail -f logs/app.log | grep "Image download"
```

### 對話狀態遺失

**症狀**: 使用者填寫到一半，系統失去對話狀態

**可能原因**:

- 應用程式重啟
- 對話狀態過期（超過 24 小時）
- 資料庫連線問題

**解決方式**:

- 重新啟動對話流程（發送「回報問題」）
- 檢查 `LineConversationStates` 表是否有記錄
- 確認 `ExpiresAt` 欄位未超過當前時間

## 單元測試執行

### 執行所有測試

```bash
dotnet test
```

### 執行特定測試專案

```bash
# 單元測試
dotnet test Tests/ClarityDesk.UnitTests/ClarityDesk.UnitTests.csproj

# 整合測試
dotnet test Tests/ClarityDesk.IntegrationTests/ClarityDesk.IntegrationTests.csproj
```

### 執行 LINE 相關測試

```bash
dotnet test --filter "FullyQualifiedName~LineMessaging"
```

### 測試覆蓋率

```bash
dotnet test --collect:"XPlat Code Coverage"
```

## 部署準備

### 1. 生產環境設定

建立 `appsettings.Production.json`：

```json
{
  "LineMessaging": {
    "ChannelAccessToken": "#{LINE_CHANNEL_ACCESS_TOKEN}#",
    "ChannelSecret": "#{LINE_CHANNEL_SECRET}#",
    "WebhookPath": "/api/line/webhook",
    "ImageUploadPath": "wwwroot/uploads/line-images",
    "MaxImageSizeBytes": 10485760,
    "MaxImagesPerIssue": 3
  }
}
```

使用 CI/CD 管道替換 `#{}#` 標記為實際金鑰。

### 2. 更新 LINE Webhook URL

將 Webhook URL 更新為生產環境網址：

```text
https://claritydesk.yourdomain.com/api/line/webhook
```

### 3. HTTPS 憑證

確保生產環境使用有效的 HTTPS 憑證（LINE 要求）。

### 4. 資料庫遷移

```bash
# 生成 SQL 腳本（在生產環境執行）
dotnet ef migrations script --output migrations.sql

# 或直接在生產環境執行
dotnet ef database update --connection "YOUR_PRODUCTION_CONNECTION_STRING"
```

### 5. 建立上傳目錄

```bash
# 在生產伺服器上執行
mkdir -p /var/www/claritydesk/wwwroot/uploads/line-images
chown www-data:www-data /var/www/claritydesk/wwwroot/uploads/line-images
```

### 6. 啟用背景服務（可選）

如有實作 `ConversationCleanupService`，確認在 `Program.cs` 已註冊：

```csharp
builder.Services.AddHostedService<ConversationCleanupService>();
```

## 監控與日誌

### 查看 LINE 推送日誌

```sql
-- 查詢最近 24 小時的推送記錄
SELECT * FROM LinePushLogs
WHERE PushedAt > DATEADD(hour, -24, GETUTCDATE())
ORDER BY PushedAt DESC;

-- 查詢失敗的推送
SELECT * FROM LinePushLogs
WHERE Status = 'Failed'
ORDER BY PushedAt DESC;
```

### 查看對話狀態

```sql
-- 查詢進行中的對話
SELECT * FROM LineConversationStates
WHERE ExpiresAt > GETUTCDATE()
ORDER BY CreatedAt DESC;
```

### 應用程式日誌

日誌位置（依 `appsettings.json` 設定）：

- 開發環境: 控制台輸出
- 生產環境: `logs/app-{Date}.log`

關鍵日誌訊息：

```text
[INFO] LINE webhook received: {EventType}
[INFO] Push message sent to {LineUserId}: {Success}
[ERROR] Failed to download LINE image: {MessageId}
[WARN] Conversation expired for user {LineUserId}
```

## 效能優化建議

### 1. 啟用 Response Caching

Webhook 回應應立即返回 200 OK，避免處理逾時。

### 2. 使用背景任務處理

長時間操作（圖片下載、推送重試）使用 `Task.Run` 或 `IHostedService`。

### 3. 資料庫索引

確認以下索引已建立（由 EF Core Configuration 自動建立）：

- `IX_LineBinding_LineUserId`
- `IX_LinePushLog_IssueReportId`
- `IX_LineConversationState_LineUserId`

## 安全性檢查清單

- [ ] Channel Secret 和 Access Token 使用 User Secrets 或環境變數
- [ ] Webhook 簽章驗證已啟用
- [ ] HTTPS 強制重導向已啟用
- [ ] 圖片上傳目錄權限正確設定（僅應用程式可寫）
- [ ] Rate Limiting 已實作（防止 Webhook 濫用）
- [ ] 錯誤訊息不包含敏感資訊（金鑰、內部路徑）

## 下一步

完成本指南後，您應該能夠：

- ✅ 在本地環境執行 LINE 整合功能
- ✅ 測試 Webhook 接收和對話流程
- ✅ 推送通知給已綁定使用者
- ✅ 排解常見問題

**後續開發**:

1. 實作 Phase 2 任務（見 `tasks.md`）
2. 新增更多測試案例
3. 優化 Flex Message 設計
4. 實作對話狀態自動清理背景服務

## 參考資源

- [LINE Messaging API 官方文件](https://developers.line.biz/en/docs/messaging-api/)
- [Flex Message Simulator](https://developers.line.biz/flex-simulator/)
- [ngrok 文件](https://ngrok.com/docs)
- [ASP.NET Core Minimal APIs](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis)
- [Entity Framework Core Migrations](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/)

## 支援

如遇問題，請參考：

- 專案 Wiki: [內部連結]
- Issue Tracker: [GitHub Issues]
- 開發團隊 Slack: #claritydesk-dev
