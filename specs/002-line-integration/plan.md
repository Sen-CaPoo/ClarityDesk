# Implementation Plan: [FEATURE]

**Branch**: `[###-feature-name]` | **Date**: [DATE] | **Spec**: [link]
**Input**: Feature specification from `/specs/[###-feature-name]/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

整合 LINE 官方帳號功能至 ClarityDesk 系統,包含三大核心功能:
1. **LINE 帳號綁定**: 使用 LINE Login OAuth 流程建立使用者與 LINE User ID 的關聯
2. **推送通知**: 新增回報單時透過 LINE Messaging API 主動推送訊息給指派的處理人員
3. **LINE 端回報**: 使用者可在 LINE 對話中透過互動式流程建立問題回報單

技術路徑採用 LINE Messaging API 實現雙向通訊,使用 Flex Message 格式提供視覺化訊息呈現。所有 LINE 對話狀態持久化儲存至 Azure SQL Database,確保會話資料可靠性。

## Technical Context

**Language/Version**: C# 12 / .NET Core 8.0  
**Primary Dependencies**: 
- LINE Messaging API SDK (`Line.Messaging` NuGet package) - NEEDS CLARIFICATION: 是否有官方 SDK 或需自行封裝 HTTP 呼叫
- ASP.NET Core Identity (現有)
- Entity Framework Core 8.x (現有)
- LINE Login OAuth middleware

**Storage**: Azure SQL Database (現有,使用 EF Core Code First workflow)  
**Testing**: xUnit + FluentAssertions + Moq (現有測試框架)  
**Target Platform**: Windows Server with IIS 10+ (現有部署環境)  
**Project Type**: Web application (ASP.NET Core Razor Pages - 現有架構)

**Performance Goals**:
- LINE Webhook 回應時間: < 3 秒 (LINE 平台要求 < 5 秒)
- 推送通知傳送: < 10 秒內完成 API 呼叫
- 支援 100 個並行 LINE 對話 Session 不降級

**Constraints**:
- HTTPS 端點強制性 (LINE Webhook 要求)
- LINE Messaging API 免費方案限制: 每月 500 則推送訊息 (需監控)
- Session 逾時機制: 30 分鐘自動清理
- 不使用 AutoMapper,採用 POCO 與 Extension Methods 進行 DTO 轉換
- 不使用 Redis,Session 資料直接儲存於 Azure SQL

**Scale/Scope**:
- 預期使用者數: 100-500 位已綁定 LINE 的使用者
- 單位數量: 10-50 個單位選項
- 每月回報單數量: 1,000-5,000 筆 (影響推送通知用量)

**需要研究的項目**:
1. LINE Messaging API 的 .NET SDK 選擇與整合方式 (官方 SDK vs 手動 HTTP 呼叫)
2. LINE Login OAuth 在 ASP.NET Core 中的整合方式 (自訂 AuthenticationHandler 或使用第三方套件)
3. LINE Webhook 簽章驗證的實作模式
4. Flex Message JSON 結構設計最佳實務
5. LINE API 錯誤處理與重試策略
6. Session 管理與逾時清理的背景任務實作 (Hosted Service vs Hangfire)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### I. 程式碼品質與可維護性 ✅

- **整潔架構**: LINE 功能將遵循現有三層架構 (Pages → Services → Data),新增 `ILineMessagingService`, `ILineBindingService`, `ILineConversationService` 等服務介面
- **SOLID 原則**: 每個服務職責單一,透過介面注入依賴,符合開放封閉原則
- **命名慣例**: 遵循現有專案 C# 命名規範 (PascalCase 公開成員, _camelCase 私有欄位)
- **可空參考型別**: 新增實體將明確標註可空性 (例如 `LineBinding.DisplayName?`)
- **依賴注入**: 所有服務透過 ASP.NET Core DI 容器註冊為 `Scoped` 生命週期
- **組態管理**: LINE API 憑證儲存於 `appsettings.json` 的 `LineSettings` 區段,搭配強型別 Options 類別
- **錯誤處理**: 實作自訂 `LineApiException` 處理 LINE API 錯誤,記錄日誌後不影響核心流程

### II. 測試優先開發 ✅

- **TDD 循環**: 所有新服務方法將先撰寫單元測試 (xUnit),測試通過後才實作
- **單元測試**: 目標覆蓋率 ≥ 80%,測試範圍包含:
  - `LineBindingService`: 綁定/解綁邏輯、重複綁定驗證
  - `LineMessagingService`: 推送訊息建構、API 呼叫模擬
  - `LineConversationService`: Session 狀態管理、資料驗證
- **整合測試**: 測試 LINE Webhook 端點、OAuth Callback 流程、與資料庫互動
- **測試獨立性**: 使用 In-Memory Database 確保測試可獨立執行
- **命名慣例**: 遵循 `[Method]_[Scenario]_[ExpectedResult]` (例如 `CreateBinding_DuplicateLineUserId_ThrowsException`)

### III. 使用者體驗一致性 ✅

- **響應式設計**: 綁定頁面與管理頁面使用現有 Bootstrap 5 樣式,支援桌面/平板/行動裝置
- **無障礙性**: 
  - 「綁定 LINE」按鈕提供鍵盤導覽與 ARIA 標籤
  - QR Code 圖片包含替代文字說明
  - 表單驗證錯誤向螢幕閱讀器宣告
- **視覺一致性**: 使用 `_Layout.cshtml` 共享版面配置,按鈕樣式遵循現有設計系統
- **表單驗證**: 
  - 客戶端: jQuery Validation 驗證電話號碼格式
  - 伺服器端: Data Annotations 與 PageModel ModelState 驗證
- **載入狀態**: 推送通知非同步處理,頁面提供「發送中」視覺回饋
- **錯誤處理**: LINE API 錯誤顯示使用者友善訊息 (例如「LINE 通知發送失敗,請稍後再試」)

### IV. 效能與可擴展性 ✅

- **回應時間標準**:
  - LINE Webhook 端點: < 3 秒 (符合 LINE 平台 5 秒限制)
  - 推送通知 API 呼叫: 非同步處理,不阻塞回報單建立流程
  - 資料庫查詢: 對 `LineBinding` 與 `LineConversationSession` 建立索引
- **資源限制**:
  - Session 資料儲存於資料庫,避免記憶體過度使用
  - 逾時 Session 透過背景任務定期清理 (每小時執行一次)
- **最佳化要求**:
  - 查詢已綁定使用者時使用 `AsNoTracking()` 提升效能
  - LINE Webhook 請求使用簽章驗證避免惡意請求消耗資源
  - 推送訊息使用 `HttpClient` 連線池,避免 Socket 耗盡
- **監控**:
  - 記錄 LINE API 呼叫次數與回應時間至結構化日誌
  - 自訂 Metric 追蹤每日推送訊息數量與失敗率
  - 監控 Session 清理效能,避免資料庫鎖定

### V. 文件與溝通 ✅

- **文件語言**: 所有規格、計畫、使用者手冊使用繁體中文
- **程式碼註解**: 公開服務介面與複雜邏輯提供繁體中文 XML 註解
- **使用者介面**: 所有 LINE 對話訊息、網頁按鈕文字使用繁體中文
- **API 文件**: 在 `contracts/` 目錄提供繁體中文服務合約說明
- **技術文件**: 更新 `docs/` 目錄的部署指南,新增 LINE 整合設定章節

**Phase 1 設計完成後重新評估結果**: ✅ 無憲法違規項目

所有設計決策均符合專案憲法要求:
- 資料模型遵循 EF Core Code First 慣例與索引最佳化原則
- 服務介面遵循 SOLID 原則,每個服務職責明確
- DTO 使用 `record` 類型確保不可變性
- 所有公開方法提供 XML 註解與參數驗證
- 錯誤處理策略明確區分預期錯誤與非預期錯誤
- 測試策略涵蓋單元測試、整合測試與 TDD 循環

## Project Structure

### Documentation (this feature)

```
specs/002-line-integration/
├── plan.md              # 本文件 (實作計畫)
├── spec.md              # 功能規格說明 (來源文件)
├── research.md          # Phase 0 技術研究報告
├── data-model.md        # Phase 1 資料模型設計
├── quickstart.md        # Phase 1 快速開始指南
├── contracts/           # Phase 1 API 合約定義
│   ├── README.md        # 合約定義總覽
│   ├── services/        # 服務介面定義
│   │   └── SERVICE-INTERFACES.md  # 所有服務介面完整定義
│   ├── dtos/            # DTO 定義 (包含在 SERVICE-INTERFACES.md)
│   └── endpoints/       # API 端點定義 (包含在 SERVICE-INTERFACES.md)
└── tasks.md             # Phase 2 任務分解 (由 /speckit.tasks 產生,尚未建立)
```

### Source Code (repository root)

此功能擴充現有 ASP.NET Core Razor Pages 架構,不改變專案結構,僅新增 LINE 相關元件:

```
ClarityDesk/ (專案根目錄)
├── Models/
│   ├── Entities/
│   │   ├── LineBinding.cs                 # 新增: LINE 綁定實體
│   │   ├── LineConversationSession.cs     # 新增: 對話 Session 實體
│   │   ├── LineMessageLog.cs              # 新增: 訊息日誌實體
│   │   └── IssueReport.cs                 # 修改: 新增 Source 欄位
│   ├── DTOs/
│   │   ├── LineBindingDto.cs              # 新增: 綁定相關 DTO
│   │   ├── LineMessageDto.cs              # 新增: 訊息相關 DTO
│   │   └── LineConversationDto.cs         # 新增: 對話相關 DTO
│   ├── Enums/
│   │   ├── BindingStatus.cs               # 新增: 綁定狀態列舉
│   │   ├── ConversationStep.cs            # 新增: 對話步驟列舉
│   │   ├── LineMessageType.cs             # 新增: 訊息類型列舉
│   │   └── IssueReportSource.cs           # 新增: 回報來源列舉
│   └── Extensions/
│       ├── LineBindingExtensions.cs       # 新增: 綁定實體/DTO 轉換
│       ├── LineMessageExtensions.cs       # 新增: 訊息實體/DTO 轉換
│       └── LineConversationExtensions.cs  # 新增: Session 實體/DTO 轉換
│
├── Data/
│   ├── ApplicationDbContext.cs            # 修改: 新增 LineBindings, LineConversationSessions, LineMessageLogs DbSet
│   └── Configurations/
│       ├── LineBindingConfiguration.cs    # 新增: EF Core 實體配置
│       ├── LineConversationSessionConfiguration.cs  # 新增
│       ├── LineMessageLogConfiguration.cs # 新增
│       └── IssueReportConfiguration.cs    # 修改: 新增 Source 欄位配置
│
├── Services/
│   ├── Interfaces/
│   │   ├── ILineBindingService.cs         # 新增: 綁定服務介面
│   │   ├── ILineMessagingService.cs       # 新增: 訊息服務介面
│   │   ├── ILineConversationService.cs    # 新增: 對話服務介面
│   │   └── ILineWebhookHandler.cs         # 新增: Webhook 處理介面
│   ├── LineBindingService.cs              # 新增: 綁定服務實作
│   ├── LineMessagingService.cs            # 新增: 訊息服務實作
│   ├── LineConversationService.cs         # 新增: 對話服務實作
│   ├── LineWebhookHandler.cs              # 新增: Webhook 處理實作
│   ├── IssueReportService.cs              # 修改: 新增回報單時呼叫推送通知
│   └── Exceptions/
│       ├── LineBindingException.cs        # 新增: 綁定例外
│       └── LineApiException.cs            # 新增: LINE API 例外
│
├── Infrastructure/
│   ├── Middleware/
│   │   └── LineSignatureValidationMiddleware.cs  # 新增: Webhook 簽章驗證中介軟體
│   ├── BackgroundServices/
│   │   └── LineSessionCleanupService.cs   # 新增: Session 清理背景服務
│   └── Authentication/
│       └── LineOAuthHandler.cs            # 新增: LINE Login OAuth 處理 (可選,視實作方式)
│
├── Pages/
│   ├── Account/
│   │   ├── LineBinding.cshtml             # 新增: LINE 綁定管理頁面
│   │   ├── LineBinding.cshtml.cs          # 新增: PageModel
│   │   └── Login.cshtml.cs                # 修改: 整合 LINE Login 選項 (可選)
│   ├── Admin/
│   │   └── LineManagement/                # 新增: 管理員功能目錄
│   │       ├── Index.cshtml               # 新增: 綁定列表頁面
│   │       ├── Index.cshtml.cs            # 新增: PageModel
│   │       └── Logs.cshtml                # 新增: 訊息日誌查詢頁面
│   └── Api/                                # 新增: API 端點目錄 (或使用 Minimal API)
│       └── LineWebhook.cs                 # 新增: LINE Webhook 端點
│
├── Migrations/
│   └── YYYYMMDDHHMMSS_AddLineIntegrationEntities.cs  # 新增: Migration 檔案
│
├── Tests/
│   └── ClarityDesk.UnitTests/
│       └── Services/
│           ├── LineBindingServiceTests.cs  # 新增: 綁定服務單元測試
│           ├── LineMessagingServiceTests.cs  # 新增: 訊息服務單元測試
│           ├── LineConversationServiceTests.cs  # 新增: 對話服務單元測試
│           └── LineWebhookHandlerTests.cs  # 新增: Webhook 處理單元測試
│
├── wwwroot/
│   └── js/
│       └── line-validation.js             # 新增: 客戶端驗證擴充 (電話號碼格式)
│
├── appsettings.json                       # 修改: 新增 LineSettings 區段結構
├── appsettings.Development.json           # 修改: 新增開發環境 LINE 設定 (無實際憑證)
└── Program.cs                             # 修改: 註冊 LINE 服務、Middleware、背景服務
```

**結構決策**: 

1. **整合至現有架構**: 遵循專案現有的三層架構 (Pages → Services → Data),不引入新的專案或模組,降低複雜度

2. **服務層隔離**: 所有 LINE 相關業務邏輯封裝於獨立服務中 (`LineBindingService`, `LineMessagingService` 等),與現有服務解耦

3. **Middleware 模式**: Webhook 簽章驗證使用自訂 Middleware,確保安全性與關注點分離

4. **背景服務**: Session 清理使用 ASP.NET Core Hosted Service,無需引入 Hangfire 等第三方任務排程器

5. **API 端點位置**: Webhook 端點可選擇以下任一方式實作:
   - **Minimal API** (推薦): 在 `Program.cs` 中使用 `app.MapPost("/api/line/webhook", ...)`
   - **Razor Pages API Handler**: 建立 `Pages/Api/LineWebhook.cshtml.cs` 作為 PageModel
   - **Controller**: 建立 `Controllers/LineWebhookController.cs` (不推薦,專案主要使用 Razor Pages)

本設計選擇 **Minimal API** 方式,與專案輕量化原則一致。

## Complexity Tracking

*此區段僅在 Constitution Check 有違規時填寫*

**評估結果**: 無違規項目,無需記錄例外情況。

所有設計決策均在專案憲法允許的範圍內:
- 未引入新的專案或模組 (保持單一 ASP.NET Core 專案)
- 未使用過度工程化的模式 (例如 CQRS, Event Sourcing)
- 使用原生 .NET 功能 (Hosted Service, Data Protection API) 而非第三方解決方案
- DTO 轉換使用 Extension Methods 而非 AutoMapper (符合專案約定)
- 快取使用 `IMemoryCache` 而非 Redis (符合專案約定)

