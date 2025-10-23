# 快速開始指南: LINE 官方帳號整合功能

**Feature Branch**: `002-line-integration`  
**Last Updated**: 2025-10-23  
**Estimated Setup Time**: 30-45 分鐘

本指南提供從零開始設定與開發 LINE 整合功能的完整步驟,包含環境準備、開發流程與測試驗證。

---

## 前置需求檢查清單

在開始開發前,請確認以下項目已完成:

- [x] 已安裝 .NET 8 SDK (`dotnet --version` 應顯示 8.x.x)
- [x] 已安裝 Visual Studio 2022 或 Visual Studio Code
- [x] 已設定 Azure SQL Database 連線字串
- [x] 已取得 LINE Developers Console 帳號
- [x] 本地開發環境可透過 HTTPS 存取 (ngrok 或類似工具,用於 Webhook 測試)
- [x] 已安裝 Git 且位於 `002-line-integration` 分支

---

## 第一階段: LINE Developers Console 設定

### 1.1 建立 LINE 官方帳號

1. 前往 [LINE Developers Console](https://developers.line.biz/console/)
2. 點擊「Create a new provider」建立提供者 (例如: "ClarityDesk")
3. 在提供者下方點擊「Create a channel」→ 選擇「Messaging API」
4. 填寫必要資訊:
   - **Channel name**: `ClarityDesk Bot`
   - **Channel description**: `ClarityDesk 問題回報追蹤系統官方帳號`
   - **Category**: `Business tools`
   - **Subcategory**: `Project management`
   - **Email address**: 您的聯絡信箱

### 1.2 設定 Messaging API

1. 進入剛建立的 Channel → 切換至「Messaging API」分頁
2. 啟用以下設定:
   - **Use webhooks**: `Enabled`
   - **Auto-reply messages**: `Disabled` (重要! 避免與自訂回覆衝突)
   - **Greeting messages**: `Disabled` (或自訂歡迎訊息)
3. 發行 **Channel access token (long-lived)**:
   - 點擊「Issue」按鈕
   - 複製並妥善保存此 Token (後續設定需要)

### 1.3 設定 LINE Login

1. 回到 Channel 首頁 → 切換至「LINE Login」分頁
2. 啟用 LINE Login 功能
3. 設定 **Callback URL**:
   - 開發環境: `https://localhost:5001/signin-line` (本地測試)
   - 正式環境: `https://your-domain.com/signin-line` (部署後設定)
4. 記錄以下憑證:
   - **Channel ID**: 位於 Channel 首頁「Basic settings」
   - **Channel secret**: 位於 Channel 首頁「Basic settings」
   - **Channel access token**: 剛才發行的 Token

---

## 第二階段: 本地開發環境設定

### 2.1 安裝 NuGet 套件

在專案根目錄執行:

```powershell
# 安裝 LINE Messaging API SDK
dotnet add package Line.Messaging --version 1.4.5

# 安裝 ASP.NET Core Data Protection (用於 Token 加密)
dotnet add package Microsoft.AspNetCore.DataProtection --version 8.0.0

# (如需要) 安裝 Polly (用於錯誤重試,可選)
# dotnet add package Polly --version 8.0.0
```

### 2.2 設定 User Secrets (開發環境)

**重要**: 絕對不要將 LINE 憑證提交到版本控制!

```powershell
# 初始化 User Secrets
dotnet user-secrets init

# 設定 LINE 憑證
dotnet user-secrets set "LineSettings:ChannelId" "YOUR_CHANNEL_ID"
dotnet user-secrets set "LineSettings:ChannelSecret" "YOUR_CHANNEL_SECRET"
dotnet user-secrets set "LineSettings:ChannelAccessToken" "YOUR_CHANNEL_ACCESS_TOKEN"

# 設定 Webhook 路徑
dotnet user-secrets set "LineSettings:WebhookPath" "/api/line/webhook"

# 設定配額限制
dotnet user-secrets set "LineSettings:MonthlyPushLimit" "500"
dotnet user-secrets set "LineSettings:SessionTimeoutMinutes" "30"
```

### 2.3 更新 appsettings.json

在 `appsettings.json` 新增以下設定結構 (不含實際憑證):

```json
{
  "LineSettings": {
    "ChannelId": "",
    "ChannelSecret": "",
    "ChannelAccessToken": "",
    "WebhookPath": "/api/line/webhook",
    "MonthlyPushLimit": 500,
    "SessionTimeoutMinutes": 30,
    "CallbackPath": "/signin-line"
  }
}
```

### 2.4 建立資料庫 Migration

```powershell
# 建立新的 Migration
dotnet ef migrations add AddLineIntegrationEntities

# 檢視生成的 SQL (可選)
dotnet ef migrations script --output migration.sql

# 套用至資料庫
dotnet ef database update
```

---

## 第三階段: 開發工作流程

### 3.1 開發優先順序 (TDD 方式)

遵循以下順序實作功能,每個步驟都先寫測試:

#### Phase 1: 綁定功能 (P1)
```
1. 建立實體類別 (LineBinding, LineConversationSession, LineMessageLog)
2. 建立 EF Core Configuration
3. 撰寫 ILineBindingService 測試
4. 實作 LineBindingService
5. 建立綁定頁面 (Pages/Account/LineBinding.cshtml)
6. 整合 LINE Login OAuth
```

#### Phase 2: 推送通知 (P2)
```
1. 撰寫 ILineMessagingService 測試
2. 實作 LineMessagingService
3. 建立 Flex Message Builder
4. 整合至 IssueReportService (新增回報單時呼叫)
5. 實作配額監控機制
```

#### Phase 3: LINE 端回報 (P3)
```
1. 撰寫 ILineConversationService 測試
2. 實作 LineConversationService
3. 撰寫 ILineWebhookHandler 測試
4. 實作 LineWebhookHandler
5. 建立 Webhook API 端點
6. 實作背景清理服務
```

### 3.2 測試驅動開發範例

```csharp
// 範例: 測試綁定服務 (Tests/ClarityDesk.UnitTests/Services/LineBindingServiceTests.cs)
public class LineBindingServiceTests
{
    [Fact]
    public async Task CreateOrUpdateBindingAsync_NewBinding_ReturnsBindingId()
    {
        // Arrange
        var dbContext = CreateInMemoryDbContext();
        var service = new LineBindingService(dbContext, _logger, _cache);
        
        // Act
        var bindingId = await service.CreateOrUpdateBindingAsync(
            userId: 1,
            lineUserId: "U1234567890abcdef",
            displayName: "測試使用者"
        );
        
        // Assert
        bindingId.Should().BeGreaterThan(0);
        var binding = await dbContext.LineBindings.FindAsync(bindingId);
        binding.Should().NotBeNull();
        binding!.UserId.Should().Be(1);
        binding.LineUserId.Should().Be("U1234567890abcdef");
    }
    
    [Fact]
    public async Task CreateOrUpdateBindingAsync_DuplicateLineUserId_ThrowsException()
    {
        // Arrange
        var dbContext = CreateInMemoryDbContext();
        await dbContext.LineBindings.AddAsync(new LineBinding
        {
            UserId = 1,
            LineUserId = "U1234567890abcdef",
            DisplayName = "已存在的綁定"
        });
        await dbContext.SaveChangesAsync();
        
        var service = new LineBindingService(dbContext, _logger, _cache);
        
        // Act & Assert
        await Assert.ThrowsAsync<LineBindingException>(() =>
            service.CreateOrUpdateBindingAsync(2, "U1234567890abcdef", "測試"));
    }
}
```

---

## 第四階段: 本地測試

### 4.1 設定 ngrok (Webhook 測試)

LINE Webhook 需要 HTTPS 端點,使用 ngrok 將本地端點公開:

```powershell
# 安裝 ngrok (https://ngrok.com/)
# 啟動 ngrok 隧道 (對應 ASP.NET Core 預設 Port)
ngrok http https://localhost:5001
```

記錄 ngrok 提供的 HTTPS URL (例如: `https://abc123.ngrok-free.app`)

### 4.2 更新 LINE Developers Console Webhook URL

1. 前往 LINE Developers Console → 您的 Channel → Messaging API
2. 設定 **Webhook URL**: `https://abc123.ngrok-free.app/api/line/webhook`
3. 點擊「Verify」按鈕測試連線 (應顯示 Success)
4. 啟用「Use webhook」開關

### 4.3 執行應用程式

```powershell
# 清理並建置
dotnet clean
dotnet build

# 執行應用程式
dotnet run

# 或使用 Visual Studio F5 偵錯模式
```

### 4.4 測試綁定流程

1. 開啟瀏覽器: `https://localhost:5001`
2. 登入系統 (非訪客帳號)
3. 前往「個人設定」或「LINE 綁定」頁面
4. 點擊「綁定 LINE 官方帳號」按鈕
5. 掃描 QR Code 或開啟 LINE 加入好友連結
6. 在 LINE 中加入 ClarityDesk Bot 為好友
7. 返回網頁,確認顯示「已綁定」狀態

### 4.5 測試推送通知

1. 在網頁端建立新的問題回報單
2. 指派給已綁定 LINE 的處理人員
3. 確認該人員的 LINE 立即收到推送訊息
4. 驗證訊息格式正確且包含所有必要資訊
5. 點擊「查看回報單詳情」按鈕,確認可正確開啟頁面

### 4.6 測試 LINE 端回報

1. 在 LINE 中向 ClarityDesk Bot 傳送「回報問題」
2. 依照機器人提示逐步輸入資訊
3. 確認送出後系統回覆回報單編號
4. 在網頁端確認回報單已成功建立

---

## 第五階段: 單元測試與整合測試

### 5.1 執行所有測試

```powershell
# 執行所有測試
dotnet test

# 執行特定測試專案
dotnet test Tests/ClarityDesk.UnitTests/ClarityDesk.UnitTests.csproj

# 產生覆蓋率報告
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

### 5.2 覆蓋率目標

確認以下服務達到 ≥ 80% 覆蓋率:
- `LineBindingService`
- `LineMessagingService`
- `LineConversationService`
- `LineWebhookHandler`

### 5.3 整合測試檢查清單

- [ ] Webhook 端點回應 200 OK
- [ ] 簽章驗證失敗時回應 401 Unauthorized
- [ ] Follow 事件正確更新綁定狀態
- [ ] Unfollow 事件標記為 Blocked
- [ ] 對話流程可完整執行至建立回報單
- [ ] Session 逾時後自動清理

---

## 第六階段: 部署至 IIS

### 6.1 準備部署檔案

```powershell
# 發佈至 Release 模式
dotnet publish -c Release -o ./publish

# 複製發佈檔案至 IIS 目錄 (例如: C:\inetpub\wwwroot\ClarityDesk)
```

### 6.2 設定 IIS 環境變數

在 IIS Manager 中設定應用程式集區的環境變數:

```
ASPNETCORE_ENVIRONMENT=Production
LineSettings__ChannelId=YOUR_CHANNEL_ID
LineSettings__ChannelSecret=YOUR_CHANNEL_SECRET
LineSettings__ChannelAccessToken=YOUR_CHANNEL_ACCESS_TOKEN
```

### 6.3 更新 LINE Webhook URL

將 LINE Developers Console 的 Webhook URL 更新為正式環境:
```
https://your-production-domain.com/api/line/webhook
```

### 6.4 驗證部署

1. 確認網站可透過 HTTPS 存取
2. 測試 Webhook 端點: LINE Developers Console → Verify 按鈕
3. 執行完整的綁定、推送、LINE 端回報流程

---

## 常見問題排解

### Q1: Webhook 驗證失敗 (401 Unauthorized)

**原因**: 簽章驗證錯誤  
**解決方法**:
1. 確認 `LineSettings:ChannelSecret` 正確無誤
2. 檢查 Middleware 是否正確讀取請求 Body
3. 使用 LINE Developers Console 的「Verify」功能測試

### Q2: 推送訊息失敗 (403 Forbidden)

**原因**: Channel Access Token 無效或過期  
**解決方法**:
1. 重新發行 Channel Access Token
2. 更新 User Secrets 或環境變數
3. 重新啟動應用程式

### Q3: LINE 端回報流程中斷

**原因**: Session 逾時或異常清除  
**解決方法**:
1. 調整 `SessionTimeoutMinutes` 設定 (建議 30 分鐘)
2. 檢查背景清理服務是否過於頻繁執行
3. 查看應用程式日誌確認錯誤訊息

### Q4: 本地測試時 ngrok 連線失敗

**原因**: 防火牆或 SSL 憑證問題  
**解決方法**:
1. 確認防火牆允許 Port 5001 流量
2. 使用 `dotnet dev-certs https --trust` 信任本地憑證
3. 嘗試使用 `--host-header=localhost:5001` 參數啟動 ngrok

---

## 效能最佳化建議

1. **快取單位清單**: 
   ```csharp
   _cache.GetOrCreateAsync("departments", async entry =>
   {
       entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
       return await _dbContext.Departments.ToListAsync();
   });
   ```

2. **非同步推送通知**: 
   使用 `Task.Run()` 或 Hangfire 避免阻塞回報單建立流程

3. **資料庫索引**: 
   確認 Migration 已建立所有必要索引 (參考 data-model.md)

4. **HttpClient 連線池**: 
   使用 `IHttpClientFactory` 管理 LINE API 連線

---

## 安全性檢查清單

- [ ] 所有 LINE 憑證儲存於 User Secrets 或環境變數,絕不提交到 Git
- [ ] Webhook 端點強制驗證簽章
- [ ] 快速連結使用時效性 Token 保護
- [ ] 訪客帳號無法使用 LINE 綁定功能
- [ ] 敏感日誌資訊 (例如 Token) 已遮罩或移除
- [ ] HTTPS 強制啟用 (HTTP 導向至 HTTPS)

---

## 後續步驟

完成本指南後,您應該已具備:
✅ 完整的 LINE 整合開發環境  
✅ 可運作的綁定、推送、LINE 端回報功能  
✅ 通過單元測試與整合測試  
✅ 部署至 IIS 的正式環境

接下來可參考:
- `tasks.md` - 詳細的任務分解與預估工時
- `data-model.md` - 完整的資料庫結構說明
- `contracts/` - 所有服務介面與 API 合約
- `research.md` - 技術選型決策記錄

---

**需要協助?** 查看專案 Wiki 或聯繫技術負責人。
