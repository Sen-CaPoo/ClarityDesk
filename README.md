# ClarityDesk - 顧客問題紀錄追蹤系統

<p align="center">
  <strong>簡潔高效的顧客問題管理解決方案</strong>
</p>

## 📋 專案簡介

ClarityDesk 是一個基於 ASP.NET Core 8 Razor Pages 開發的顧客問題紀錄追蹤系統,提供直觀的介面讓團隊快速記錄、追蹤和管理客戶回報的問題。系統整合 LINE Login 身份驗證,讓使用者可以快速登入並開始使用。

## ✨ 功能特色

### 核心功能

- **回報單管理** (使用者故事 1)
  - 建立、編輯、刪除回報單
  - 多條件篩選 (狀態、優先級、日期範圍、關鍵字)
  - 分頁顯示,支援大量資料
  - 即時統計資訊 (待處理、處理中、已完成)
  - 支援多來源回報 (網頁端/LINE Bot)

- **LINE 整合功能** ✨ **NEW**
  - **LINE 官方帳號綁定**
    - 透過 LINE Login OAuth 流程完成帳號綁定
    - 支援綁定狀態管理 (已綁定/已封鎖/已解綁)
    - 自動同步 LINE 顯示名稱與頭像
    - 訪客帳號無法使用綁定功能
  
  - **回報單推送通知**
    - 新回報單建立時自動推送通知給指派處理人員
    - 採用 LINE Flex Message 精美卡片式設計
    - 包含完整回報單資訊 (編號、標題、緊急程度、單位、聯絡人等)
    - 提供快速連結按鈕,一鍵開啟回報單詳細頁面
    - 非同步推送,不阻塞回報單建立流程
    - 自動檢查綁定狀態與 API 配額限制
  
  - **LINE 端互動式回報問題**
    - 在 LINE 對話中輸入「回報問題」即可啟動流程
    - 智慧對話引導,逐步填寫問題資訊
    - 支援快速回覆按鈕 (單位選擇、緊急程度選擇)
    - 即時輸入驗證 (電話格式、必填欄位檢查)
    - 顯示摘要確認,防止誤提交
    - 自動填入回報人與紀錄日期
    - 根據所屬單位自動指派預設處理人員
    - 支援隨時輸入「取消」中斷流程
    - Session 持久化儲存,30 分鐘自動過期
  
  - **管理功能**
    - 查看所有使用者的 LINE 綁定狀態
    - 強制解除使用者綁定
    - 訊息發送日誌查詢 (支援多條件篩選)
    - LINE API 使用量監控 (每月配額、每日統計)
    - 配額警告機制 (達 80% 時提醒管理員)

- **LINE Login 整合** (使用者故事 2)
  - 透過 LINE 帳號快速註冊與登入
  - 自動同步 LINE 個人資料 (頭像、顯示名稱)
  - 安全的 OAuth 2.0 流程
  - 支援遊客模式 (無需 LINE 帳號即可體驗)

- **使用者權限管理** (使用者故事 3)
  - 角色管理 (普通使用者 / 管理人員)
  - 帳號啟用/停用控制
  - 管理員專屬功能保護
  - 查看使用者 LINE 綁定狀態

- **問題所屬單位維護** (使用者故事 4)
  - 自訂問題分類單位
  - 為單位指派預設處理人員
  - 軟刪除機制,保留歷史資料
  - LINE 端回報自動帶入單位清單

### 技術亮點

- **響應式設計**: 支援桌面、平板、手機多種螢幕尺寸 (320px - 1920px)
- **商務白風格**: 簡潔專業的 UI 設計,使用淺藍色點綴
- **效能最佳化**: 
  - Response Compression (Gzip/Brotli)
  - 靜態檔案快取 (365 天)
  - 資料庫索引最佳化
  - 記憶體快取 (統計資訊、單位清單、綁定狀態)
  - 非同步訊息推送,不阻塞主流程
- **安全性**: 
  - HTTPS 強制跳轉
  - XSS/CSRF 防護
  - 安全標頭設定
  - LINE Webhook 簽章驗證 (HMAC-SHA256)
  - OAuth 2.0 安全認證
- **整合能力**:
  - LINE Messaging API 完整整合
  - LINE Login OAuth 2.0
  - Webhook 事件處理 (關注、封鎖、訊息接收)
  - LINE SDK 最佳實踐應用

## 🛠 技術堆疊

### 後端
- **框架**: ASP.NET Core 8.0 (Razor Pages)
- **語言**: C# 12
- **資料庫**: Azure SQL Database
- **ORM**: Entity Framework Core 8.0 (Code First)
- **快取**: IMemoryCache (ASP.NET Core 內建)
- **LINE SDK**: LINE Messaging API SDK (官方套件)

### 前端
- **UI 框架**: Bootstrap 5.3
- **JavaScript**: jQuery 3.7 + 原生 JavaScript
- **驗證**: jQuery Validation + Bootstrap Validation

### 身份驗證
- **OAuth 2.0**: LINE Login
- **Session**: Cookie-based Authentication

### 外部整合
- **LINE Messaging API**: 訊息推送、Webhook 事件處理
- **LINE Login API**: OAuth 2.0 使用者認證

### 測試
- **單元測試**: xUnit
- **斷言庫**: FluentAssertions
- **模擬庫**: Moq

### 部署
- **平台**: Windows Server + IIS 10.0+
- **環境**: .NET 8.0 Runtime

## 🚀 快速開始

### 先決條件

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [SQL Server](https://www.microsoft.com/sql-server) 或 [Azure SQL Database](https://azure.microsoft.com/services/sql-database/)
- [LINE Developers Account](https://developers.line.biz/) (用於 LINE Login 與 Messaging API)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) 或 [VS Code](https://code.visualstudio.com/)

### 1. 複製專案

```bash
git clone https://github.com/Sen-CaPoo/ClarityDesk.git
cd ClarityDesk
```

### 2. 設定資料庫連線

編輯 `appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=your-server.database.windows.net;Database=ClarityDesk;User Id=your-username;Password=your-password;Encrypt=True;"
  }
}
```

### 3. 設定 LINE Login 與 LINE 整合

#### 3.1 建立 LINE Channel

1. 前往 [LINE Developers Console](https://developers.line.biz/console/)
2. 建立新的 **Provider** (若尚未建立)
3. 建立新的 **Channel** 並選擇 **Messaging API**
4. 完成 Channel 基本資訊設定

#### 3.2 取得必要憑證

在 Channel 設定頁面取得以下資訊:

- **Basic settings**:
  - `Channel ID`: 用於 LINE Login 與綁定識別
  - `Channel secret`: 用於 API 請求簽章驗證

- **Messaging API**:
  - `Channel access token`: 用於發送訊息 (點擊 "Issue" 按鈕產生)
  - `Bot basic ID`: 用於產生加好友連結 (格式: `@xxx`)

#### 3.3 設定 Callback URL 與 Webhook URL

在 LINE Developers Console 中設定:

1. **LINE Login** 標籤頁:
   - **Callback URL**: `https://your-domain.com/signin-line`
   - 將 `your-domain.com` 替換為您的實際網域

2. **Messaging API** 標籤頁:
   - **Webhook URL**: `https://your-domain.com/api/line/webhook`
   - 啟用 **Use webhook** 選項
   - 停用 **Auto-reply messages** (避免與 Bot 邏輯衝突)
   - 停用 **Greeting messages** (可選)

⚠️ **開發環境注意事項**: 
- 本地開發時,LINE 無法呼叫 `localhost`,需使用 [ngrok](https://ngrok.com/) 或 [localtunnel](https://localtunnel.me/) 等工具建立公開 URL
- 範例: `ngrok http 5001` 會產生類似 `https://abc123.ngrok.io` 的 URL,將此 URL 設定到 LINE Console

#### 3.4 編輯應用程式設定檔

編輯 `appsettings.Development.json`:

```json
{
  "LineLogin": {
    "ChannelId": "your-line-channel-id",
    "ChannelSecret": "your-line-channel-secret"
  },
  "LineSettings": {
    "ChannelId": "your-line-channel-id",
    "ChannelSecret": "your-line-channel-secret",
    "ChannelAccessToken": "your-line-channel-access-token",
    "BotBasicId": "xxx",
    "WebhookPath": "/api/line/webhook",
    "CallbackPath": "/signin-line",
    "MonthlyPushLimit": 500,
    "SessionTimeoutMinutes": 30
  }
}
```

**設定說明**:
- `MonthlyPushLimit`: LINE 免費方案每月推送訊息配額 (預設 500 則)
- `SessionTimeoutMinutes`: LINE 端回報對話 Session 逾時時間 (預設 30 分鐘)
- `BotBasicId`: 不含 `@` 符號,僅填寫 ID 部分

⚠️ **安全性提醒**: 
- 在生產環境中,**絕對不要**將憑證直接寫在設定檔中
- 使用環境變數或 Azure Key Vault 儲存敏感資訊
- 參考「使用 User Secrets」段落進行本地開發設定

#### 3.5 使用 User Secrets (建議)

在開發環境使用 .NET User Secrets 安全儲存憑證:

```bash
# 初始化 User Secrets
dotnet user-secrets init

# 設定 LINE Login 憑證
dotnet user-secrets set "LineLogin:ChannelId" "your-channel-id"
dotnet user-secrets set "LineLogin:ChannelSecret" "your-channel-secret"

# 設定 LINE Messaging API 憑證
dotnet user-secrets set "LineSettings:ChannelId" "your-channel-id"
dotnet user-secrets set "LineSettings:ChannelSecret" "your-channel-secret"
dotnet user-secrets set "LineSettings:ChannelAccessToken" "your-access-token"
dotnet user-secrets set "LineSettings:BotBasicId" "xxx"
```

使用 User Secrets 後,這些設定會覆蓋 `appsettings.json` 中的對應值,且不會被提交到版本控制。

### 4. 執行 Migration

```bash
dotnet ef database update
```

### 5. 執行專案

```bash
dotnet run
```

開啟瀏覽器訪問 `https://localhost:5001`

### 6. 預設帳號

系統會自動建立預設管理員帳號與 3 個預設單位:
- **單位**: 客服部、技術部、業務部
- **管理員**: 首次透過 LINE Login 登入後,需手動升級為管理員角色

## 📖 使用說明

### 普通使用者操作流程

1. **登入系統**
   - 點擊「使用 LINE 登入」
   - 授權 LINE Login 權限
   - 自動建立使用者帳號
   - 或選擇「以訪客身份體驗」快速進入系統

2. **綁定 LINE 官方帳號** (可選,用於接收推送通知與使用 LINE 回報功能)
   - 前往「個人資料」→「LINE 綁定管理」頁面
   - 點擊「綁定 LINE 官方帳號」按鈕
   - 系統觸發 LINE OAuth 流程,確認授權
   - 綁定完成後,掃描頁面顯示的 QR Code 或點擊連結加入 ClarityDesk 官方帳號為好友
   - 系統顯示「已綁定」狀態,包含 LINE 顯示名稱與綁定時間
   - 若需解除綁定,點擊「解除綁定」按鈕並確認

3. **建立回報單 (網頁端)**
   - 導覽至「回報單管理」
   - 點擊「新增回報單」
   - 填寫必要資訊:
     - 問題標題 (必填)
     - 問題內容描述 (必填)
     - 顧客聯絡人姓名 (必填)
     - 顧客聯絡電話 (必填)
     - 選擇緊急程度 (低/中/高)
     - 選擇問題所屬單位
     - 指派處理人員 (系統自動帶入該單位的預設處理人員,可手動修改)
   - 確認送出
   - 若處理人員已綁定 LINE,該人員會立即在 LINE 收到推送通知

4. **在 LINE 中回報問題** (若已綁定)
   - 在 LINE 中開啟 ClarityDesk Bot 聊天室
   - 傳送「回報問題」關鍵字或點擊選單按鈕
   - 系統啟動互動式對話流程:
     1. **問題標題**: 輸入問題的簡短標題
     2. **問題內容**: 詳細描述問題狀況
     3. **所屬單位**: 點擊快速回覆按鈕選擇單位 (系統自動載入所有啟用中的單位)
     4. **緊急程度**: 選擇 🔴 高 / 🟡 中 / 🟢 低
     5. **顧客姓名**: 輸入顧客的聯絡人姓名
     6. **顧客電話**: 輸入顧客的聯絡電話 (系統會驗證格式)
   - 系統顯示摘要確認頁面,包含所有填寫資訊與自動填入的資訊:
     - 回報人: 從綁定帳號自動帶入
     - 紀錄日期: 系統當前時間
     - 指派處理人員: 根據所屬單位自動指派
   - 確認送出後,系統回覆回報單編號與查看連結
   - 回報單立即同步至網頁端,可在「回報單管理」中查看
   - 指派的處理人員 (若已綁定 LINE) 同樣會收到推送通知
   - **中斷流程**: 在任何步驟輸入「取消」可立即終止回報流程

5. **管理回報單**
   - 檢視所有回報單列表
   - 使用篩選條件快速找到特定回報單:
     - 處理狀態 (待處理/處理中/已完成)
     - 緊急程度 (低/中/高)
     - 日期範圍
     - 關鍵字搜尋 (標題/內容/聯絡人)
     - 回報來源 (網頁端/LINE Bot)
   - 點擊回報單查看完整詳細資訊
   - 編輯回報單資訊 (標題、內容、狀態、處理人員等)
   - 更新處理狀態: 待處理 → 處理中 → 已完成
   - 刪除不需要的回報單

### 管理人員操作流程

1. **使用者權限管理**
   - 導覽至「系統管理」→「使用者權限管理」
   - 查看所有註冊使用者清單,包含:
     - 使用者名稱、角色、啟用狀態
     - LINE 綁定狀態 (已綁定/未綁定)
     - 最後登入時間
   - 變更使用者角色 (普通使用者 ↔ 管理人員)
   - 啟用/停用使用者帳號

2. **問題所屬單位維護**
   - 導覽至「系統管理」→「問題所屬單位維護」
   - 查看所有單位清單與啟用狀態
   - 新增單位:
     - 輸入單位名稱
     - 選擇預設處理人員
   - 編輯單位:
     - 修改單位名稱
     - 變更預設處理人員
   - 刪除單位 (軟刪除,僅標記為停用以保留歷史資料)

3. **LINE 管理功能** ✨ **NEW**
   
   #### 3.1 綁定狀態管理
   - 導覽至「系統管理」→「LINE 管理」→「綁定狀態」
   - 查看所有使用者的 LINE 綁定資訊:
     - ClarityDesk 使用者名稱
     - LINE 顯示名稱
     - LINE User ID
     - 綁定狀態 (已綁定/已封鎖/已解綁)
     - 綁定時間
     - 最後互動時間
   - 篩選功能:
     - 依綁定狀態篩選
     - 搜尋使用者名稱或 LINE 顯示名稱
   - 強制解除綁定 (管理員權限):
     - 點擊「解除綁定」按鈕
     - 確認後立即解除該使用者的 LINE 綁定
   - 分頁顯示,預設每頁 20 筆
   
   #### 3.2 訊息日誌查詢
   - 導覽至「系統管理」→「LINE 管理」→「訊息日誌」
   - 查看所有 LINE 訊息發送記錄:
     - 發送時間
     - LINE 使用者 (顯示名稱 + User ID)
     - 訊息類型 (文字/Flex Message/快速回覆)
     - 訊息方向 (接收/發送)
     - 發送狀態 (成功/失敗)
     - 關聯回報單編號 (若適用)
     - 錯誤訊息 (若發送失敗)
   - 多條件篩選:
     - 依 LINE User ID 篩選
     - 依訊息方向篩選 (接收/發送)
     - 依發送狀態篩選 (成功/失敗)
     - 依日期範圍篩選
   - 分頁顯示,預設每頁 50 筆
   - 匯出 CSV (可選功能,未來實作)
   
   #### 3.3 API 使用量監控
   - 導覽至「系統管理」→「LINE 管理」→「使用量統計」
   - **當月配額使用情況**:
     - 已使用推送訊息數量
     - 配額限制 (預設 500 則/月)
     - 使用率百分比
     - 視覺化進度條顯示
     - 配額警告 (達 80% 時顯示橘色警告,達 100% 時顯示紅色)
   - **每日發送統計** (最近 30 天):
     - 日期
     - 成功發送數量
     - 失敗發送數量
     - 圖表視覺化 (可選功能,未來實作)
   - **重要提醒**:
     - LINE 免費方案每月推送訊息配額為 500 則
     - 回覆訊息 (Reply API) 不計入配額
     - 超過配額後無法發送推送通知,但不影響回報單建立與 Webhook 接收
   
   #### 3.4 系統健康度檢查
   - 檢查 LINE Channel Access Token 有效性
   - 檢查 Webhook URL 連線狀態
   - 檢查過期 Session 數量
   - 提供一鍵清理過期 Session 功能

## 🧪 測試

### 執行單元測試

```bash
dotnet test Tests/ClarityDesk.UnitTests
```

### 執行整合測試

```bash
dotnet test Tests/ClarityDesk.IntegrationTests
```

### 測試覆蓋率

```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

### LINE 整合功能測試

LINE 整合功能包含完整的單元測試與整合測試:

**單元測試** (Tests/ClarityDesk.UnitTests/Services/):
- `LineBindingServiceTests.cs`: 綁定管理邏輯測試
- `LineMessagingServiceTests.cs`: 訊息發送邏輯測試
- `LineConversationServiceTests.cs`: 對話流程邏輯測試
- `LineWebhookHandlerTests.cs`: Webhook 事件處理測試

**整合測試** (Tests/ClarityDesk.IntegrationTests/):
- `LineWebhookIntegrationTests.cs`: 完整 Webhook 請求流程測試 (使用 TestServer)

**測試覆蓋範圍**:
- 綁定建立、查詢、解除
- 訊息推送成功與失敗場景
- 對話流程各步驟轉換
- Webhook 簽章驗證
- 配額檢查機制
- Session 過期清理

執行 LINE 相關測試:
```bash
# 執行所有 LINE 相關測試
dotnet test --filter "FullyQualifiedName~Line"

# 執行特定服務測試
dotnet test --filter "FullyQualifiedName~LineBindingServiceTests"
```

## � LINE 整合功能詳細說明

### 功能架構

LINE 整合功能採用模組化設計,包含以下核心元件:

```
使用者 (LINE App)
    ↕ Webhook Events
LINE Platform (Messaging API)
    ↕ Push/Reply Messages
ClarityDesk Backend
    ├─ LineWebhookHandler (Webhook 事件處理)
    ├─ LineMessagingService (訊息發送)
    ├─ LineBindingService (綁定管理)
    ├─ LineConversationService (對話管理)
    └─ LineUsageMonitorService (用量監控)
```

### 三大核心功能

#### 1️⃣ LINE 官方帳號綁定

**技術實作**:
- 使用 LINE Login OAuth 2.0 流程
- 建立 ClarityDesk User ID ↔ LINE User ID 映射關係
- 支援綁定狀態管理 (Active/Blocked/Unbound)
- 自動更新最後互動時間

**安全機制**:
- 一個 LINE 帳號只能綁定一個 ClarityDesk 帳號
- 訪客帳號無法使用綁定功能
- 管理員可強制解除異常綁定

**資料表**: `LineBindings`
- 主鍵: `Id`
- 唯一索引: `LineUserId`
- 外鍵: `UserId` → `Users.Id`

#### 2️⃣ 回報單推送通知

**觸發時機**:
- 網頁端建立新回報單時
- LINE 端建立新回報單時 (處理人員收到通知)

**推送邏輯**:
1. 檢查指派處理人員是否已綁定 LINE
2. 檢查綁定狀態是否為 `Active`
3. 檢查當月推送配額是否足夠
4. 建構 Flex Message 訊息卡片
5. 呼叫 LINE Push Message API
6. 記錄發送結果到 `LineMessageLogs`

**Flex Message 設計**:
```json
{
  "type": "bubble",
  "header": { "type": "box", "contents": [ "新問題回報" ] },
  "body": {
    "type": "box",
    "contents": [
      { "type": "text", "text": "回報單編號:#20251023-001" },
      { "type": "text", "text": "問題標題:系統異常" },
      { "type": "text", "text": "緊急程度:🔴 高" },
      { "type": "text", "text": "問題所屬單位:資訊部" },
      { "type": "text", "text": "聯絡人:王小明" },
      { "type": "text", "text": "連絡電話:0912-345-678" },
      { "type": "text", "text": "紀錄日期:2025/10/23 14:30" },
      { "type": "text", "text": "回報人:陳大華" }
    ]
  },
  "footer": {
    "type": "box",
    "contents": [
      {
        "type": "button",
        "action": {
          "type": "uri",
          "label": "查看回報單詳情",
          "uri": "https://your-domain.com/Issues/Details/123"
        }
      }
    ]
  }
}
```

**錯誤處理**:
- 推送失敗不影響回報單建立
- 記錄錯誤訊息與 LINE API 錯誤代碼
- 使用者封鎖 Bot 時自動更新綁定狀態為 `Blocked`

**配額管理**:
- LINE 免費方案: 每月 500 則推送訊息
- Reply API 不計入配額
- 達 80% 時顯示警告
- 超過配額時停止推送並記錄日誌

#### 3️⃣ LINE 端互動式回報問題

**對話流程設計**:

```
使用者輸入「回報問題」
    ↓
建立 LineConversationSession (狀態: AwaitingTitle)
    ↓
使用者輸入問題標題
    ↓
驗證輸入 → 更新 Session (狀態: AwaitingContent)
    ↓
使用者輸入問題內容
    ↓
顯示單位選項 (Quick Reply) → 更新 Session (狀態: AwaitingDepartment)
    ↓
使用者選擇單位
    ↓
顯示緊急程度選項 (Quick Reply) → 更新 Session (狀態: AwaitingUrgency)
    ↓
使用者選擇緊急程度
    ↓
詢問顧客姓名 → 更新 Session (狀態: AwaitingContactPerson)
    ↓
使用者輸入顧客姓名
    ↓
詢問顧客電話 → 更新 Session (狀態: AwaitingPhoneNumber)
    ↓
使用者輸入電話 (驗證格式)
    ↓
顯示摘要確認 (狀態: AwaitingConfirmation)
    ↓
使用者確認送出
    ↓
建立 IssueReport (Source = LineBot)
    ↓
刪除 Session & 回覆回報單編號
```

**Session 管理**:
- 儲存位置: `LineConversationSessions` 資料表
- Session 資料: JSON 格式 (title, content, departmentId, urgency, contactPerson, phoneNumber)
- 過期機制: 建立後 30 分鐘自動過期
- 清理機制: 背景服務 `SessionCleanupService` 每小時清理過期 Session

**輸入驗證**:
- 問題標題: 1-100 字元
- 問題內容: 1-1000 字元
- 電話號碼: 正則表達式驗證 (支援 `0912345678`, `0912-345-678`, `(02)1234-5678` 等格式)
- 單位/緊急程度: 從資料庫查詢有效選項

**中斷機制**:
- 使用者輸入「取消」可隨時終止流程
- 系統回覆「已取消回報流程」並刪除 Session

### Webhook 事件處理

**支援的事件類型**:

| 事件類型 | 處理邏輯 |
|---------|---------|
| `follow` | 使用者加入好友,標記綁定狀態為 `Active` |
| `unfollow` | 使用者封鎖 Bot,標記綁定狀態為 `Blocked` |
| `message` | 接收文字訊息,處理對話流程或指令 |
| `postback` | 處理快速回覆按鈕點擊事件 |

**安全驗證**:
- 自訂 Middleware: `LineWebhookSignatureValidationMiddleware`
- 驗證方式: HMAC-SHA256
- 驗證內容: Request Body + Channel Secret
- 簽章位置: `X-Line-Signature` Header
- 驗證失敗回傳: `401 Unauthorized`

**Webhook 端點**:
- URL: `POST /api/line/webhook`
- Content-Type: `application/json`
- 需要 HTTPS (生產環境)

### 使用量監控

**監控指標**:
- 當月推送訊息數量 (Push Message API)
- 每日發送統計 (成功/失敗)
- 配額使用率百分比
- 配額警告機制

**資料來源**: `LineMessageLogs` 資料表
- 篩選條件: `Direction = Outbound` AND `MessageType = Text/FlexMessage`
- 統計週期: 每月 1 日 00:00 重置

**警告機制**:
- 80% 配額: 橘色警告,提醒管理員注意
- 100% 配額: 紅色警告,停止推送並記錄日誌
- 通知方式: 管理後台顯示 + 日誌記錄

### 背景服務

**SessionCleanupService** (`Infrastructure/BackgroundServices/SessionCleanupService.cs`):
- 執行頻率: 每小時
- 清理邏輯: 刪除 `ExpiresAt < DateTime.UtcNow` 的 Session
- 日誌記錄: 記錄清理數量
- 使用技術: `IHostedService` + `Timer`

### 設定參數說明

```json
{
  "LineSettings": {
    "ChannelId": "LINE Channel ID",
    "ChannelSecret": "用於 Webhook 簽章驗證",
    "ChannelAccessToken": "用於發送訊息的存取令牌",
    "BotBasicId": "官方帳號 ID (不含 @)",
    "WebhookPath": "/api/line/webhook",
    "CallbackPath": "/signin-line",
    "MonthlyPushLimit": 500,          // 每月推送配額
    "SessionTimeoutMinutes": 30       // Session 過期時間
  }
}
```

### 資料庫 Schema 變更

**新增資料表** (Migration: `20251023163553_AddLineIntegrationEntities`):

1. **LineBindings**: 綁定關係
   - 主鍵: `Id` (int)
   - 唯一索引: `LineUserId`
   - 外鍵: `UserId` → `Users.Id`

2. **LineConversationSessions**: 對話 Session
   - 主鍵: `Id` (Guid)
   - 索引: `LineUserId`, `ExpiresAt`
   - 外鍵: `UserId` → `Users.Id`

3. **LineMessageLogs**: 訊息日誌
   - 主鍵: `Id` (Guid)
   - 複合索引: (`LineUserId`, `SentAt`)
   - 外鍵: `IssueReportId` → `IssueReports.Id` (可選)

**修改資料表**:
- `IssueReports`: 新增 `Source` 欄位 (enum: Web/LineBot)

### 故障排除

**常見問題**:

1. **Webhook 接收不到事件**
   - 檢查 LINE Console 是否正確設定 Webhook URL
   - 確認 Webhook URL 使用 HTTPS
   - 檢查防火牆是否封鎖 LINE Platform IP
   - 查看 `LineMessageLogs` 是否有錯誤記錄

2. **推送通知發送失敗**
   - 檢查 Channel Access Token 是否過期
   - 確認使用者已加入 Bot 為好友 (未加入會回傳 403)
   - 檢查當月配額是否已用完
   - 查看 `LineMessageLogs.ErrorMessage` 欄位

3. **對話流程異常中斷**
   - 檢查 Session 是否過期 (預設 30 分鐘)
   - 確認資料庫 `LineConversationSessions` 資料表可正常寫入
   - 查看應用程式日誌檔案

4. **本地開發 Webhook 測試**
   - 使用 ngrok: `ngrok http 5001`
   - 將 ngrok URL 設定到 LINE Console
   - 確認 ngrok 連線穩定 (免費版有時間限制)

### 效能最佳化

**快取策略**:
- 綁定狀態快取: 5 分鐘 (使用 `IMemoryCache`)
- 單位清單快取: 1 小時
- 快取鍵命名: `line_binding_{userId}`, `departments_active`

**非同步處理**:
- 推送通知使用 `Task.Run()` 非同步執行
- 不阻塞回報單建立流程
- 錯誤不會影響主要業務邏輯

**資料庫索引**:
- `LineBindings.LineUserId`: 唯一索引
- `LineConversationSessions.LineUserId`: 索引
- `LineConversationSessions.ExpiresAt`: 索引
- `LineMessageLogs` 複合索引: (`LineUserId`, `SentAt`)

## �📦 部署

詳細部署說明請參考 [docs/deployment/DEPLOYMENT.md](docs/deployment/DEPLOYMENT.md)

### 快速部署至 IIS

```bash
# 1. 發佈專案
dotnet publish -c Release -o ./publish

# 2. 將 publish 資料夾內容複製到 IIS 網站目錄
# 3. 確保 IIS 已安裝 ASP.NET Core Hosting Bundle
# 4. 設定應用程式集區為「無受管理的程式碼」
# 5. 重新啟動 IIS
```

完整檢查清單請見 [docs/deployment/IIS-DEPLOYMENT-CHECKLIST.md](docs/deployment/IIS-DEPLOYMENT-CHECKLIST.md)

## 📁 專案結構

```
ClarityDesk/
├── Pages/                  # Razor Pages (UI 層)
│   ├── Issues/            # 回報單管理頁面
│   ├── Admin/             # 管理功能頁面
│   │   ├── Users/         # 使用者權限管理
│   │   ├── Departments/   # 單位維護
│   │   └── LineManagement/ # LINE 管理功能 ✨ NEW
│   │       ├── Index.cshtml       # 綁定狀態管理
│   │       └── Logs.cshtml        # 訊息日誌查詢
│   ├── Account/           # 身份驗證頁面
│   │   └── LineBinding.cshtml  # LINE 綁定管理 ✨ NEW
│   └── Shared/            # 共享版面配置與元件
├── Models/                 # 資料模型
│   ├── Entities/          # EF Core 實體
│   │   ├── LineBinding.cs           # LINE 綁定實體 ✨ NEW
│   │   ├── LineConversationSession.cs # 對話 Session 實體 ✨ NEW
│   │   └── LineMessageLog.cs        # 訊息日誌實體 ✨ NEW
│   ├── DTOs/              # 資料傳輸物件
│   │   ├── LineBindingDto.cs        # 綁定 DTO ✨ NEW
│   │   ├── LineMessageDto.cs        # 訊息 DTO ✨ NEW
│   │   └── LineConversationDto.cs   # 對話 DTO ✨ NEW
│   ├── ViewModels/        # 頁面顯示模型
│   ├── Enums/             # 列舉型別
│   │   ├── BindingStatus.cs         # 綁定狀態 ✨ NEW
│   │   ├── ConversationStep.cs      # 對話步驟 ✨ NEW
│   │   ├── LineMessageType.cs       # 訊息類型 ✨ NEW
│   │   ├── MessageDirection.cs      # 訊息方向 ✨ NEW
│   │   └── IssueReportSource.cs     # 回報來源 ✨ NEW
│   └── Extensions/        # 擴充方法
│       ├── LineBindingExtensions.cs ✨ NEW
│       ├── LineMessageExtensions.cs ✨ NEW
│       └── LineConversationExtensions.cs ✨ NEW
├── Services/              # 服務層 (業務邏輯)
│   ├── Interfaces/        # 服務介面
│   │   ├── ILineBindingService.cs      ✨ NEW
│   │   ├── ILineMessagingService.cs    ✨ NEW
│   │   ├── ILineConversationService.cs ✨ NEW
│   │   ├── ILineWebhookHandler.cs      ✨ NEW
│   │   └── ILineUsageMonitorService.cs ✨ NEW
│   ├── LineBindingService.cs           ✨ NEW
│   ├── LineMessagingService.cs         ✨ NEW
│   ├── LineConversationService.cs      ✨ NEW
│   ├── LineWebhookHandler.cs           ✨ NEW
│   ├── LineUsageMonitorService.cs      ✨ NEW
│   └── IssueReportService.cs    # 整合 LINE 推送通知
├── Data/                  # 資料存取層
│   ├── Configurations/    # EF Core Entity Configurations
│   │   ├── LineBindingConfiguration.cs           ✨ NEW
│   │   ├── LineConversationSessionConfiguration.cs ✨ NEW
│   │   └── LineMessageLogConfiguration.cs        ✨ NEW
│   └── Migrations/        # EF Core Migrations
│       └── 20251023163553_AddLineIntegrationEntities.cs ✨ NEW
├── Infrastructure/        # 基礎設施層
│   ├── Authentication/    # LINE Login 相關
│   ├── Middleware/        # 自訂 Middleware
│   │   └── LineWebhookSignatureValidationMiddleware.cs ✨ NEW
│   ├── BackgroundServices/ # 背景服務
│   │   └── SessionCleanupService.cs ✨ NEW
│   └── TagHelpers/        # 自訂 Tag Helpers
├── wwwroot/               # 靜態資源
│   ├── css/              # 樣式表
│   ├── js/               # JavaScript
│   │   └── line-binding.js ✨ NEW
│   └── lib/              # 前端套件
├── docs/                  # 專案文件
│   ├── deployment/        # 部署文件
│   ├── development/       # 開發指南
│   ├── changelogs/        # 變更記錄
│   │   └── 002-line-integration.md ✨ NEW
│   ├── SDD-Docs/          # 系統設計文件
│   │   └── 2-2-specify-追加需求_LINE整合功能.md ✨ NEW
│   └── *.md              # 使用者手冊等
├── scripts/               # 腳本工具
│   └── *.ps1             # PowerShell 腳本
├── database/              # 資料庫腳本
│   └── *.sql             # SQL 腳本
├── specs/                 # 規格文件
│   ├── 001-customer-issue-tracker/  # 功能規格
│   └── 002-line-integration/ ✨ NEW  # LINE 整合規格
│       ├── spec.md        # 功能規格
│       ├── plan.md        # 實作計畫
│       ├── tasks.md       # 任務分解
│       ├── data-model.md  # 資料模型
│       ├── research.md    # 技術研究
│       └── quickstart.md  # 快速開始指南
└── Tests/                 # 測試專案
    ├── UnitTests/        # 單元測試
    │   ├── Services/
    │   │   ├── LineBindingServiceTests.cs ✨ NEW
    │   │   ├── LineMessagingServiceTests.cs ✨ NEW
    │   │   ├── LineConversationServiceTests.cs ✨ NEW
    │   │   └── LineWebhookHandlerTests.cs ✨ NEW
    └── IntegrationTests/ # 整合測試
        └── LineWebhookIntegrationTests.cs ✨ NEW
```

## 🤝 貢獻指南

詳細貢獻指南請參考 [docs/development/CONTRIBUTING.md](docs/development/CONTRIBUTING.md)

## 📚 文件目錄

- **部署文件**: [docs/deployment/](docs/deployment/) - 包含完整的部署指南與檢查清單
- **開發指南**: [docs/development/](docs/development/) - 包含貢獻指南與 AI Agent 協作指引
- **變更記錄**: [docs/changelogs/](docs/changelogs/) - 各功能的變更歷史記錄
  - [002-line-integration.md](docs/changelogs/002-line-integration.md) - LINE 整合功能完整變更記錄 ✨ NEW
- **使用者手冊**: [docs/user-manual.md](docs/user-manual.md) - 完整的使用者操作指南
- **系統設計文件**: [docs/SDD-Docs/](docs/SDD-Docs/) - 系統設計文件
  - [2-2-specify-追加需求_LINE整合功能.md](docs/SDD-Docs/2-2-specify-追加需求_LINE整合功能.md) - LINE 整合原始需求 ✨ NEW
- **規格文件**: [specs/](specs/) - 詳細的功能規格與 API 定義
  - [001-customer-issue-tracker/](specs/001-customer-issue-tracker/) - 核心功能規格
  - [002-line-integration/](specs/002-line-integration/) - LINE 整合功能規格 ✨ NEW
    - [spec.md](specs/002-line-integration/spec.md) - 功能規格與驗收標準
    - [plan.md](specs/002-line-integration/plan.md) - 實作計畫與階段劃分
    - [tasks.md](specs/002-line-integration/tasks.md) - 詳細任務分解 (150+ 任務)
    - [data-model.md](specs/002-line-integration/data-model.md) - 資料模型設計
    - [research.md](specs/002-line-integration/research.md) - 技術研究與決策記錄
    - [quickstart.md](specs/002-line-integration/quickstart.md) - LINE 整合快速開始指南

更多文件請參考 [docs/README.md](docs/README.md)

## 📝 授權

本專案採用 MIT 授權條款 - 詳見 [LICENSE](LICENSE) 檔案

## 👥 作者

- **Sen-CaPoo** - *Initial work* - [GitHub](https://github.com/Sen-CaPoo)

## 🙏 致謝

- 感謝 [ASP.NET Core](https://docs.microsoft.com/aspnet/core) 團隊提供強大的框架
- 感謝 [Bootstrap](https://getbootstrap.com/) 提供優秀的 UI 組件
- 感謝 [LINE Developers](https://developers.line.biz/) 提供 LINE Login 與 Messaging API 服務
- 感謝 [LINE Messaging API SDK](https://github.com/line/line-bot-sdk-dotnet) 提供 .NET SDK 支援

## 📞 聯絡方式

如有任何問題或建議,歡迎透過以下方式聯絡:

- **Issue**: [GitHub Issues](https://github.com/Sen-CaPoo/ClarityDesk/issues)
- **Email**: support@claritydesk.com
- **LINE 官方帳號**: @claritydesk (僅限已部署環境)

---

**ClarityDesk** - 讓問題管理變得更簡單,LINE 整合讓溝通更即時 ✨
