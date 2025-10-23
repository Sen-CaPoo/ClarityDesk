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

- **LINE 整合功能** ✨ **NEW**
  - **LINE 官方帳號綁定**: 使用者可綁定 LINE 帳號,接收即時通知
  - **推送通知**: 新回報單建立時,自動推送訊息給指派的處理人員
  - **LINE 端回報問題**: 直接在 LINE 對話中透過互動式流程建立回報單
  - **管理功能**: 管理員可查看綁定狀態、訊息日誌與 API 使用量

- **LINE Login 整合** (使用者故事 2)
  - 透過 LINE 帳號快速註冊與登入
  - 自動同步 LINE 個人資料 (頭像、顯示名稱)
  - 安全的 OAuth 2.0 流程

- **使用者權限管理** (使用者故事 3)
  - 角色管理 (普通使用者 / 管理人員)
  - 帳號啟用/停用控制
  - 管理員專屬功能保護

- **問題所屬單位維護** (使用者故事 4)
  - 自訂問題分類單位
  - 為單位指派預設處理人員
  - 軟刪除機制,保留歷史資料

### 技術亮點

- **響應式設計**: 支援桌面、平板、手機多種螢幕尺寸 (320px - 1920px)
- **商務白風格**: 簡潔專業的 UI 設計,使用淺藍色點綴
- **效能最佳化**: 
  - Response Compression (Gzip/Brotli)
  - 靜態檔案快取 (365 天)
  - 資料庫索引最佳化
  - 記憶體快取 (統計資訊、單位清單)
- **安全性**: 
  - HTTPS 強制跳轉
  - XSS/CSRF 防護
  - 安全標頭設定

## 🛠 技術堆疊

### 後端
- **框架**: ASP.NET Core 8.0 (Razor Pages)
- **語言**: C# 12
- **資料庫**: Azure SQL Database
- **ORM**: Entity Framework Core 8.0 (Code First)
- **快取**: IMemoryCache (ASP.NET Core 內建)

### 前端
- **UI 框架**: Bootstrap 5.3
- **JavaScript**: jQuery 3.7 + 原生 JavaScript
- **驗證**: jQuery Validation + Bootstrap Validation

### 身份驗證
- **OAuth 2.0**: LINE Login
- **Session**: Cookie-based Authentication

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
- [LINE Developers Account](https://developers.line.biz/) (用於 LINE Login)
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

1. 前往 [LINE Developers Console](https://developers.line.biz/console/)
2. 建立新的 Channel (Messaging API)
3. 取得 **Channel ID**、**Channel Secret** 和 **Channel Access Token**
4. 設定 LINE Login Callback URL: `https://your-domain/signin-line`
5. 設定 Webhook URL: `https://your-domain/api/line/webhook`

編輯 `appsettings.Development.json`:

```json
{
  "LineLogin": {
    "ChannelId": "your-channel-id",
    "ChannelSecret": "your-channel-secret"
  },
  "LineSettings": {
    "ChannelId": "your-channel-id",
    "ChannelSecret": "your-channel-secret",
    "ChannelAccessToken": "your-channel-access-token",
    "WebhookPath": "/api/line/webhook",
    "CallbackPath": "/signin-line",
    "MonthlyPushLimit": 500,
    "SessionTimeoutMinutes": 30
  }
}
```

⚠️ **安全性提醒**: 在生產環境中,請使用環境變數或 Azure Key Vault 儲存憑證,不要直接寫在設定檔中。

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

2. **綁定 LINE 官方帳號** (可選,用於接收推送通知)
   - 前往「LINE 綁定管理」頁面
   - 掃描 QR Code 加入 ClarityDesk 官方帳號
   - 完成綁定後可在 LINE 接收通知與回報問題

3. **建立回報單**
   - 導覽至「回報單管理」
   - 點擊「新增回報單」
   - 填寫問題標題、內容、客戶資訊
   - 選擇緊急程度與所屬單位
   - 指派處理人員 (若已綁定 LINE,處理人員會收到推送通知)

4. **在 LINE 中回報問題** (若已綁定)
   - 在 LINE 中開啟 ClarityDesk Bot 聊天室
   - 傳送「回報問題」啟動互動式流程
   - 依序填寫問題資訊
   - 確認送出後自動建立回報單

5. **管理回報單**
   - 檢視所有回報單列表
   - 使用篩選條件快速找到特定回報單
   - 編輯回報單資訊
   - 更新處理狀態 (待處理 → 處理中 → 已完成)

### 管理人員操作流程

1. **使用者權限管理**
   - 導覽至「系統管理」→「使用者權限管理」
   - 查看所有註冊使用者
   - 變更使用者角色 (普通使用者 ↔ 管理人員)
   - 啟用/停用使用者帳號

2. **問題所屬單位維護**
   - 導覽至「系統管理」→「問題所屬單位維護」
   - 新增/編輯/刪除單位
   - 為單位指派預設處理人員

3. **LINE 管理功能** ✨ **NEW**
   - 導覽至「系統管理」→「LINE 管理」
   - 檢視所有使用者的 LINE 綁定狀態
   - 查看 LINE 訊息發送歷史記錄
   - 監控 LINE API 使用量與配額狀況

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

## 📦 部署

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
│   ├── Account/           # 身份驗證頁面
│   └── Shared/            # 共享版面配置與元件
├── Models/                 # 資料模型
│   ├── Entities/          # EF Core 實體
│   ├── DTOs/              # 資料傳輸物件
│   ├── ViewModels/        # 頁面顯示模型
│   └── Enums/             # 列舉型別
├── Services/              # 服務層 (業務邏輯)
│   ├── Interfaces/        # 服務介面
│   └── *.cs              # 服務實作
├── Data/                  # 資料存取層
│   ├── Configurations/    # EF Core Entity Configurations
│   └── Migrations/        # EF Core Migrations
├── Infrastructure/        # 基礎設施層
│   ├── Authentication/    # LINE Login 相關
│   ├── Middleware/        # 自訂 Middleware
│   └── TagHelpers/        # 自訂 Tag Helpers
├── wwwroot/               # 靜態資源
│   ├── css/              # 樣式表
│   ├── js/               # JavaScript
│   └── lib/              # 前端套件
├── docs/                  # 專案文件
│   ├── deployment/        # 部署文件
│   ├── development/       # 開發指南
│   ├── changelogs/        # 變更記錄
│   └── *.md              # 使用者手冊等
├── scripts/               # 腳本工具
│   └── *.ps1             # PowerShell 腳本
├── database/              # 資料庫腳本
│   └── *.sql             # SQL 腳本
├── specs/                 # 規格文件
│   └── 001-customer-issue-tracker/  # 功能規格
└── Tests/                 # 測試專案
    ├── UnitTests/        # 單元測試
    └── IntegrationTests/ # 整合測試
```

## 🤝 貢獻指南

詳細貢獻指南請參考 [docs/development/CONTRIBUTING.md](docs/development/CONTRIBUTING.md)

## 📚 文件目錄

- **部署文件**: [docs/deployment/](docs/deployment/) - 包含完整的部署指南與檢查清單
- **開發指南**: [docs/development/](docs/development/) - 包含貢獻指南與 AI Agent 協作指引
- **變更記錄**: [docs/changelogs/](docs/changelogs/) - 各功能的變更歷史記錄
- **使用者手冊**: [docs/user-manual.md](docs/user-manual.md) - 完整的使用者操作指南
- **規格文件**: [specs/](specs/) - 詳細的功能規格與 API 定義

更多文件請參考 [docs/README.md](docs/README.md)

## 📝 授權

本專案採用 MIT 授權條款 - 詳見 [LICENSE](LICENSE) 檔案

## 👥 作者

- **Sen-CaPoo** - *Initial work* - [GitHub](https://github.com/Sen-CaPoo)

## 🙏 致謝

- 感謝 [ASP.NET Core](https://docs.microsoft.com/aspnet/core) 團隊提供強大的框架
- 感謝 [Bootstrap](https://getbootstrap.com/) 提供優秀的 UI 組件
- 感謝 [LINE Developers](https://developers.line.biz/) 提供 LINE Login 服務

## 📞 聯絡方式

如有任何問題或建議,歡迎透過以下方式聯絡:

- **Issue**: [GitHub Issues](https://github.com/Sen-CaPoo/ClarityDesk/issues)
- **Email**: support@claritydesk.com

---

**ClarityDesk** - 讓問題管理變得更簡單 ✨
