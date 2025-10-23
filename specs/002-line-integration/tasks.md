# Tasks: LINE 官方帳號整合功能

**功能分支**: `002-line-integration`  
**建立日期**: 2025-10-24  
**輸入文件**: plan.md, spec.md, data-model.md, research.md, quickstart.md, contracts/SERVICE-INTERFACES.md

**測試策略**: 本功能採用 TDD (測試驅動開發) 方式,所有服務方法均需先撰寫單元測試,測試通過後才實作。目標覆蓋率 ≥ 80%。

**組織方式**: 任務按使用者故事 (User Story) 分組,每個故事可獨立實作與測試,支援漸進式交付。

---

## 任務格式說明

- **格式**: `- [ ] [TaskID] [P?] [Story?] 描述 (檔案路徑)`
- **[P]**: 可並行執行 (不同檔案、無依賴關係)
- **[Story]**: 任務所屬的使用者故事 (例如 US1, US2, US3)
- **所有路徑**: 相對於專案根目錄 `ClarityDesk/`

---

## Phase 1: 環境設定與基礎架構 (Setup)

**目的**: 專案初始化、NuGet 套件安裝與環境配置

**估計時間**: 2-3 小時

### 套件與配置

- [X] T001 安裝 LINE Messaging API SDK (`dotnet add package Line.Messaging --version 1.4.5`)
- [X] T002 [P] 設定 User Secrets (LINE 憑證: ChannelId, ChannelSecret, ChannelAccessToken)
- [X] T003 [P] 更新 `appsettings.json` 新增 LineSettings 區段結構 (不含實際憑證)
- [X] T004 建立 LINE Developers Console Channel 並記錄憑證至文件 (參考 quickstart.md)

### 專案結構準備

- [X] T005 [P] 建立目錄結構: `Models/Enums/`, `Models/Extensions/`, `Services/Exceptions/`, `Infrastructure/BackgroundServices/`
- [X] T006 [P] 建立目錄結構: `Infrastructure/Middleware/`, `Tests/ClarityDesk.UnitTests/Services/` (若不存在)

**Checkpoint**: 開發環境已設定完成,可開始實作資料模型

---

## Phase 2: 基礎建設 (Foundational - 阻塞所有 User Story)

**目的**: 建立所有使用者故事共用的核心基礎設施,此階段必須完成後才能開始任何 User Story 實作

**⚠️ 關鍵**: 所有 User Story 工作必須等待此階段完成

**估計時間**: 8-10 小時

### 資料模型與 EF Core 配置

- [X] T007 [P] 建立列舉類別: `BindingStatus`, `ConversationStep`, `LineMessageType`, `MessageDirection`, `IssueReportSource` in `Models/Enums/`
- [X] T008 [P] 建立實體類別: `LineBinding` in `Models/Entities/LineBinding.cs`
- [X] T009 [P] 建立實體類別: `LineConversationSession` in `Models/Entities/LineConversationSession.cs`
- [X] T010 [P] 建立實體類別: `LineMessageLog` in `Models/Entities/LineMessageLog.cs`
- [X] T011 修改實體類別: 在 `IssueReport` 新增 `Source` 欄位 (`Models/Entities/IssueReport.cs`)
- [X] T012 [P] 建立 EF Core 配置: `LineBindingConfiguration` in `Data/Configurations/LineBindingConfiguration.cs`
- [X] T013 [P] 建立 EF Core 配置: `LineConversationSessionConfiguration` in `Data/Configurations/LineConversationSessionConfiguration.cs`
- [X] T014 [P] 建立 EF Core 配置: `LineMessageLogConfiguration` in `Data/Configurations/LineMessageLogConfiguration.cs`
- [X] T015 修改 EF Core 配置: 更新 `IssueReportConfiguration` 新增 Source 欄位配置 (`Data/Configurations/IssueReportConfiguration.cs`)
- [X] T016 修改 DbContext: 在 `ApplicationDbContext` 新增 `DbSet<LineBinding>`, `DbSet<LineConversationSession>`, `DbSet<LineMessageLog>` (`Data/ApplicationDbContext.cs`)
- [X] T017 建立 Migration: `dotnet ef migrations add AddLineIntegrationEntities`
- [X] T018 套用 Migration 至資料庫: `dotnet ef database update`

### DTO 與 Extension Methods

- [X] T019 [P] 建立 DTO: `LineBindingDto`, `CreateBindingRequest`, `PagedResult<T>` in `Models/DTOs/LineBindingDto.cs`
- [X] T020 [P] 建立 DTO: `LineMessageLogDto`, `QuickReplyOption` in `Models/DTOs/LineMessageDto.cs`
- [X] T021 [P] 建立 DTO: `LineConversationSessionDto`, `ConversationResponse`, `ValidationResult` in `Models/DTOs/LineConversationDto.cs`
- [X] T022 [P] 建立 Extension Methods: `LineBindingExtensions` (ToDto, ToEntity) in `Models/Extensions/LineBindingExtensions.cs`
- [X] T023 [P] 建立 Extension Methods: `LineMessageExtensions` in `Models/Extensions/LineMessageExtensions.cs`
- [X] T024 [P] 建立 Extension Methods: `LineConversationExtensions` in `Models/Extensions/LineConversationExtensions.cs`

### 自訂例外與基礎服務介面

- [X] T025 [P] 建立自訂例外: `LineBindingException` in `Services/Exceptions/LineBindingException.cs`
- [X] T026 [P] 建立自訂例外: `LineApiException` in `Services/Exceptions/LineApiException.cs`
- [X] T027 建立強型別 Options 類別: `LineSettings` in `Infrastructure/Options/LineSettings.cs`
- [X] T028 在 `Program.cs` 註冊 LineSettings (`builder.Services.Configure<LineSettings>(...)`)

**Checkpoint**: 基礎建設完成,所有 User Story 現在可以並行開始實作

---

## Phase 3: User Story 1 - LINE 官方帳號綁定 (Priority: P1) 🎯 MVP

**目標**: 實作 ClarityDesk 使用者與 LINE 帳號的綁定、解綁與狀態管理功能,這是所有 LINE 功能的基礎。

**獨立測試方式**: 登入系統 → 點擊「綁定 LINE」按鈕 → 掃描 QR Code 完成綁定 → 確認頁面顯示「已綁定」狀態及 LINE 顯示名稱 → 點擊「解除綁定」確認功能正常

**估計時間**: 12-15 小時

### 測試 (TDD - 先寫測試)

- [X] T029 [P] [US1] 建立測試檔案: `LineBindingServiceTests.cs` in `Tests/ClarityDesk.UnitTests/Services/`
- [X] T030 [P] [US1] 撰寫單元測試: `CreateOrUpdateBindingAsync_NewBinding_ReturnsBindingId`
- [X] T031 [P] [US1] 撰寫單元測試: `CreateOrUpdateBindingAsync_DuplicateLineUserId_ThrowsException`
- [X] T032 [P] [US1] 撰寫單元測試: `GetBindingByUserIdAsync_ExistingBinding_ReturnsDto`
- [X] T033 [P] [US1] 撰寫單元測試: `IsUserBoundAsync_BoundUser_ReturnsTrue`
- [X] T034 [P] [US1] 撰寫單元測試: `UnbindAsync_ExistingBinding_ReturnsTrue`
- [X] T035 [P] [US1] 撰寫單元測試: `MarkAsBlockedAsync_UpdatesStatus`
- [X] T036 [US1] 執行測試確認失敗 (`dotnet test --filter "FullyQualifiedName~LineBindingServiceTests"`)

### 服務實作

- [X] T037 [US1] 建立服務介面: `ILineBindingService` in `Services/Interfaces/ILineBindingService.cs` (參考 contracts/SERVICE-INTERFACES.md)
- [X] T038 [US1] 實作服務: `LineBindingService` in `Services/LineBindingService.cs` (實作所有介面方法)
- [X] T039 [US1] 在 `Program.cs` 註冊服務: `builder.Services.AddScoped<ILineBindingService, LineBindingService>()`
- [X] T040 [US1] 執行測試確認通過 (`dotnet test --filter "FullyQualifiedName~LineBindingServiceTests"`)

### LINE Login OAuth 整合

- [X] T041 [US1] 在 `Program.cs` 設定 LINE OAuth Provider (使用 `AddOAuth("LINE", ...)`, 參考 research.md)
- [X] T042 [US1] 實作 OAuth OnCreatingTicket 事件處理 (呼叫 `ILineBindingService.CreateOrUpdateBindingAsync`)
- [X] T043 [US1] 修改 `User` 實體新增導覽屬性: `LineBinding?` in `Models/Entities/User.cs`

### 綁定管理頁面 (Razor Pages)

- [X] T044 [P] [US1] 建立 PageModel: `LineBinding.cshtml.cs` in `Pages/Account/LineBinding.cshtml.cs`
- [X] T045 [P] [US1] 建立 View: `LineBinding.cshtml` in `Pages/Account/LineBinding.cshtml` (包含綁定按鈕、QR Code 顯示、解綁按鈕)
- [X] T046 [US1] 實作 PageModel 方法: `OnGetAsync` (載入綁定狀態)
- [X] T047 [US1] 實作 PageModel 方法: `OnPostUnbindAsync` (處理解除綁定)
- [X] T048 [US1] 在 `_Layout.cshtml` 或 `_LoginPartial.cshtml` 新增「LINE 綁定」導覽連結

### 訪客帳號限制

- [X] T049 [US1] 在 `LineBinding.cshtml` 新增訪客帳號檢查,禁用綁定按鈕
- [X] T050 [US1] 在 `LineBindingService.CreateOrUpdateBindingAsync` 新增訪客帳號驗證,拋出 `InvalidOperationException`

**Checkpoint**: User Story 1 完成,使用者可透過網頁綁定 LINE 帳號,為後續功能奠定基礎

---

## Phase 4: User Story 2 - 新增回報單時的 LINE 推送通知 (Priority: P2)

**目標**: 當系統新增回報單時,自動推送通知給已綁定 LINE 的指派處理人員,包含完整回報單摘要與快速連結。

**獨立測試方式**: 在網頁端建立新回報單 → 指派給已綁定 LINE 的處理人員 → 確認該人員 LINE 收到推送訊息 → 驗證訊息包含所有必要資訊 (回報單編號、標題、緊急程度、單位、聯絡人、電話) → 點擊「查看詳情」按鈕可正確開啟頁面

**估計時間**: 10-12 小時

### 測試 (TDD - 先寫測試)

- [X] T051 [P] [US2] 建立測試檔案: `LineMessagingServiceTests.cs` in `Tests/ClarityDesk.UnitTests/Services/`
- [X] T052 [P] [US2] 撰寫單元測試: `SendIssueNotificationAsync_ValidData_ReturnsTrue`
- [X] T053 [P] [US2] 撰寫單元測試: `SendIssueNotificationAsync_LineApiError_ReturnsFalse`
- [X] T054 [P] [US2] 撰寫單元測試: `BuildIssueNotificationFlexMessage_ReturnsValidJson`
- [X] T055 [P] [US2] 撰寫單元測試: `CanSendPushMessageAsync_BelowLimit_ReturnsTrue`
- [X] T056 [P] [US2] 撰寫單元測試: `CanSendPushMessageAsync_ExceedLimit_ReturnsFalse`
- [X] T057 [US2] 執行測試確認失敗 (`dotnet test --filter "FullyQualifiedName~LineMessagingServiceTests"`)

### 服務實作

- [X] T058 [US2] 建立服務介面: `ILineMessagingService` in `Services/Interfaces/ILineMessagingService.cs`
- [X] T059 [US2] 實作服務: `LineMessagingService` in `Services/LineMessagingService.cs`
- [X] T060 [US2] 實作方法: `BuildIssueNotificationFlexMessage` (建構 Flex Message JSON,參考 research.md 範例)
- [X] T061 [US2] 實作方法: `SendIssueNotificationAsync` (呼叫 LINE Messaging API)
- [X] T062 [US2] 實作方法: `CanSendPushMessageAsync` (檢查配額限制)
- [X] T063 [US2] 實作方法: `LogMessageAsync` (記錄訊息日誌至 `LineMessageLog`)
- [X] T064 [US2] 在 `Program.cs` 註冊服務: `builder.Services.AddScoped<ILineMessagingService, LineMessagingService>()`
- [X] T065 [US2] 註冊 `ILineMessagingClient` (LINE SDK): `builder.Services.AddSingleton<ILineMessagingClient>(...)`
- [X] T066 [US2] 執行測試確認通過 (`dotnet test --filter "FullyQualifiedName~LineMessagingServiceTests"`)

### Token 安全機制

- [X] T067 [P] [US2] 建立服務介面: `IIssueReportTokenService` in `Services/Interfaces/IIssueReportTokenService.cs`
- [X] T068 [P] [US2] 實作服務: `IssueReportTokenService` (使用 Data Protection API,參考 research.md)
- [X] T069 [US2] 在 `Program.cs` 註冊服務: `builder.Services.AddScoped<IIssueReportTokenService, IssueReportTokenService>()`
- [X] T070 [US2] 修改 `Pages/Issues/Details.cshtml.cs` 新增 Token 驗證邏輯

### 整合至回報單建立流程

- [X] T071 [US2] 修改 `IssueReportService.CreateIssueReportAsync` 方法 (在建立回報單後呼叫推送通知)
- [X] T072 [US2] 實作推送邏輯: 檢查處理人員綁定狀態 → 呼叫 `ILineMessagingService.SendIssueNotificationAsync`
- [X] T073 [US2] 確保推送失敗不影響回報單建立 (使用 try-catch 包裝)

### 配額監控機制

- [X] T074 [P] [US2] 建立服務介面: `ILineUsageMonitorService` in `Services/Interfaces/ILineUsageMonitorService.cs`
- [X] T075 [P] [US2] 實作服務: `LineUsageMonitorService` (參考 research.md)
- [X] T076 [US2] 在 `Program.cs` 註冊服務: `builder.Services.AddScoped<ILineUsageMonitorService, LineUsageMonitorService>()`
- [X] T077 [US2] 整合至 `LineMessagingService`: 發送前檢查配額,發送後記錄用量

**Checkpoint**: User Story 2 完成,處理人員可在 LINE 即時收到回報單通知,大幅提升回應速度

---

## Phase 5: User Story 3 - LINE 端回報問題 (Priority: P3)

**目標**: 實作使用者在 LINE 對話中透過互動式流程回報問題,系統引導逐步填寫資訊並自動建立回報單。

**獨立測試方式**: 在 LINE 中輸入「回報問題」→ 依序輸入問題標題、內容、選擇單位、選擇緊急程度、輸入聯絡人、輸入電話 → 確認摘要正確 → 送出 → 收到回報單編號 → 在網頁端確認回報單已成功建立

**估計時間**: 16-20 小時

### 測試 (TDD - 先寫測試)

- [ ] T078 [P] [US3] 建立測試檔案: `LineConversationServiceTests.cs` in `Tests/ClarityDesk.UnitTests/Services/`
- [ ] T079 [P] [US3] 撰寫單元測試: `StartConversationAsync_NewSession_ReturnsSessionId`
- [ ] T080 [P] [US3] 撰寫單元測試: `StartConversationAsync_ExistingSession_ThrowsException`
- [ ] T081 [P] [US3] 撰寫單元測試: `ProcessUserInputAsync_ValidTitle_AdvancesToNextStep`
- [ ] T082 [P] [US3] 撰寫單元測試: `ValidateInput_InvalidPhoneNumber_ReturnsInvalid`
- [ ] T083 [P] [US3] 撰寫單元測試: `CompleteConversationAsync_ValidData_ReturnsIssueId`
- [ ] T084 [P] [US3] 撰寫單元測試: `CancelConversationAsync_RemovesSession`
- [ ] T085 [P] [US3] 撰寫單元測試: `CleanupExpiredSessionsAsync_RemovesExpiredOnly`
- [ ] T086 [US3] 執行測試確認失敗 (`dotnet test --filter "FullyQualifiedName~LineConversationServiceTests"`)

### 服務實作 - 對話管理

- [ ] T087 [US3] 建立服務介面: `ILineConversationService` in `Services/Interfaces/ILineConversationService.cs`
- [ ] T088 [US3] 實作服務: `LineConversationService` in `Services/LineConversationService.cs`
- [ ] T089 [US3] 實作方法: `StartConversationAsync` (建立新 Session,設定過期時間 30 分鐘)
- [ ] T090 [US3] 實作方法: `GetActiveSessionAsync` (查詢使用者進行中的 Session)
- [ ] T091 [US3] 實作方法: `ProcessUserInputAsync` (處理使用者輸入,推進對話步驟)
- [ ] T092 [US3] 實作方法: `ValidateInput` (驗證電話號碼格式等,參考 research.md)
- [ ] T093 [US3] 實作方法: `UpdateSessionDataAsync` (更新暫存資料至 JSON 欄位)
- [ ] T094 [US3] 實作方法: `CompleteConversationAsync` (建立回報單並清除 Session)
- [ ] T095 [US3] 實作方法: `CancelConversationAsync` (清除 Session)
- [ ] T096 [US3] 實作方法: `CleanupExpiredSessionsAsync` (背景清理用)
- [ ] T097 [US3] 在 `Program.cs` 註冊服務: `builder.Services.AddScoped<ILineConversationService, LineConversationService>()`
- [ ] T098 [US3] 執行測試確認通過 (`dotnet test --filter "FullyQualifiedName~LineConversationServiceTests"`)

### 測試 (TDD - Webhook Handler)

- [ ] T099 [P] [US3] 建立測試檔案: `LineWebhookHandlerTests.cs` in `Tests/ClarityDesk.UnitTests/Services/`
- [ ] T100 [P] [US3] 撰寫單元測試: `ValidateSignature_ValidSignature_ReturnsTrue`
- [ ] T101 [P] [US3] 撰寫單元測試: `ValidateSignature_InvalidSignature_ReturnsFalse`
- [ ] T102 [P] [US3] 撰寫單元測試: `HandleFollowEventAsync_NewUser_CreatesBinding`
- [ ] T103 [P] [US3] 撰寫單元測試: `HandleUnfollowEventAsync_UpdatesStatusToBlocked`
- [ ] T104 [P] [US3] 撰寫單元測試: `HandleMessageEventAsync_TriggerKeyword_StartsConversation`
- [ ] T105 [US3] 執行測試確認失敗 (`dotnet test --filter "FullyQualifiedName~LineWebhookHandlerTests"`)

### 服務實作 - Webhook 處理

- [ ] T106 [US3] 建立服務介面: `ILineWebhookHandler` in `Services/Interfaces/ILineWebhookHandler.cs`
- [ ] T107 [US3] 實作服務: `LineWebhookHandler` in `Services/LineWebhookHandler.cs`
- [ ] T108 [US3] 實作方法: `ValidateSignature` (HMAC-SHA256 簽章驗證,參考 research.md)
- [ ] T109 [US3] 實作方法: `HandleWebhookAsync` (解析 JSON,分派事件至對應處理方法)
- [ ] T110 [US3] 實作方法: `HandleFollowEventAsync` (使用者加入好友時更新綁定狀態)
- [ ] T111 [US3] 實作方法: `HandleUnfollowEventAsync` (使用者封鎖時標記狀態)
- [ ] T112 [US3] 實作方法: `HandleMessageEventAsync` (處理使用者訊息,識別「回報問題」關鍵字)
- [ ] T113 [US3] 在 `Program.cs` 註冊服務: `builder.Services.AddScoped<ILineWebhookHandler, LineWebhookHandler>()`
- [ ] T114 [US3] 執行測試確認通過 (`dotnet test --filter "FullyQualifiedName~LineWebhookHandlerTests"`)

### Webhook 端點與 Middleware

- [ ] T115 [US3] 建立 Middleware: `LineSignatureValidationMiddleware` in `Infrastructure/Middleware/LineSignatureValidationMiddleware.cs` (參考 research.md)
- [ ] T116 [US3] 在 `Program.cs` 註冊 Middleware: `app.UseMiddleware<LineSignatureValidationMiddleware>()`
- [ ] T117 [US3] 建立 Minimal API 端點: `app.MapPost("/api/line/webhook", ...)` in `Program.cs`
- [ ] T118 [US3] 實作端點處理邏輯: 讀取 Body → 呼叫 `ILineWebhookHandler.HandleWebhookAsync` → 回應 200 OK

### 背景清理服務

- [ ] T119 [US3] 建立背景服務: `LineSessionCleanupService` in `Infrastructure/BackgroundServices/LineSessionCleanupService.cs` (參考 research.md)
- [ ] T120 [US3] 實作 `ExecuteAsync` 方法: 每小時執行一次清理
- [ ] T121 [US3] 在 `Program.cs` 註冊背景服務: `builder.Services.AddHostedService<LineSessionCleanupService>()`

### 整合測試 (可選,若時間充足)

- [ ] T122 [P] [US3] 建立整合測試檔案: `LineWebhookIntegrationTests.cs` in `Tests/ClarityDesk.IntegrationTests/`
- [ ] T123 [P] [US3] 撰寫整合測試: 模擬完整 Webhook 請求流程 (使用 TestServer)
- [ ] T124 [US3] 執行整合測試確認通過

**Checkpoint**: User Story 3 完成,使用者可在 LINE 中直接回報問題,提供最大便利性

---

## Phase 6: 管理介面與日誌查詢 (Optional Enhancement)

**目的**: 提供管理員查看所有綁定狀態與訊息日誌的管理介面

**估計時間**: 6-8 小時

- [ ] T125 [P] 建立 PageModel: `Index.cshtml.cs` in `Pages/Admin/LineManagement/Index.cshtml.cs`
- [ ] T126 [P] 建立 View: `Index.cshtml` in `Pages/Admin/LineManagement/Index.cshtml` (顯示綁定列表)
- [ ] T127 [P] 建立 PageModel: `Logs.cshtml.cs` in `Pages/Admin/LineManagement/Logs.cshtml.cs`
- [ ] T128 [P] 建立 View: `Logs.cshtml` in `Pages/Admin/LineManagement/Logs.cshtml` (顯示訊息日誌)
- [ ] T129 實作分頁與篩選邏輯 (狀態、日期範圍)
- [ ] T130 在 `Program.cs` 設定授權: `options.Conventions.AuthorizePage("/Admin/LineManagement/Index", "Admin")`

---

## Phase 7: 效能優化與安全性強化 (Polish & Cross-Cutting)

**目的**: 改善整體效能、安全性與可維護性

**估計時間**: 4-6 小時

### 效能優化

- [ ] T131 [P] 實作單位清單快取 (用於 LINE 快速回覆按鈕): `IMemoryCache` 快取 1 小時
- [ ] T132 [P] 實作綁定狀態快取: 快取 5 分鐘,避免頻繁查詢資料庫
- [ ] T133 在所有唯讀查詢使用 `AsNoTracking()` 提升效能
- [ ] T134 [P] 使用 `IHttpClientFactory` 管理 LINE API 連線 (避免 Socket 耗盡)

### 安全性強化

- [ ] T135 [P] 確認所有 LINE 憑證已移除 `appsettings.json`,僅保留結構
- [ ] T136 [P] 在日誌記錄中遮罩敏感資訊 (Channel Access Token, LINE User ID 部分字元)
- [ ] T137 [P] 實作 Rate Limiting (防止 Webhook 端點被濫用)
- [ ] T138 確認 HTTPS 強制啟用 (HTTP 自動導向至 HTTPS)

### 程式碼品質

- [ ] T139 [P] Code Review: 檢查所有服務是否遵循命名慣例與錯誤處理模式
- [ ] T140 [P] 新增 XML 註解至所有公開服務介面與方法
- [ ] T141 [P] 重構重複邏輯為共用方法 (例如日誌記錄、錯誤處理)

### 文件更新

- [ ] T142 [P] 更新 `docs/user-manual.md` 新增 LINE 整合功能使用說明
- [ ] T143 [P] 更新 `docs/deployment/DEPLOYMENT.md` 新增 LINE 整合環境設定章節
- [ ] T144 [P] 建立變更記錄: `docs/changelogs/002-line-integration.md`
- [ ] T145 [P] 更新專案 README.md 新增 LINE 整合功能說明

### 驗證與部署準備

- [ ] T146 執行所有測試並確認覆蓋率 ≥ 80%: `dotnet test /p:CollectCoverage=true`
- [ ] T147 執行 `quickstart.md` 所有驗證步驟,確認功能完整性
- [ ] T148 在本地環境完整測試三個 User Story 的獨立性與整合性
- [ ] T149 準備部署檔案: `dotnet publish -c Release -o ./publish`
- [ ] T150 更新 LINE Developers Console Webhook URL 為正式環境位址

---

## 依賴關係與執行順序

### 階段依賴

- **Phase 1 (Setup)**: 無依賴,可立即開始
- **Phase 2 (Foundational)**: 依賴 Phase 1 完成,**阻塞所有 User Story**
- **Phase 3 (US1)**: 依賴 Phase 2 完成,無其他 User Story 依賴
- **Phase 4 (US2)**: 依賴 Phase 2 完成,**依賴 US1** (需要綁定功能才能推送)
- **Phase 5 (US3)**: 依賴 Phase 2 完成,**依賴 US1** (需要綁定功能才能識別使用者),**依賴 US2** (回報單建立後需推送通知)
- **Phase 6 (管理介面)**: 依賴所有 User Story 完成 (可選)
- **Phase 7 (Polish)**: 依賴所有期望的 User Story 完成

### User Story 依賴關係

```
US1 (LINE 綁定)
     │
     ├─→ US2 (推送通知) - 需要查詢綁定狀態
     │
     └─→ US3 (LINE 端回報) - 需要識別使用者身份 + 推送通知
```

**建議執行順序**: Phase 1 → Phase 2 → US1 → US2 → US3 → Phase 6 (可選) → Phase 7

### Story 內部依賴

#### User Story 1 (LINE 綁定)
1. 測試 (T029-T036) → 執行確認失敗 (T036)
2. 服務介面與實作 (T037-T039) → 執行確認通過 (T040)
3. OAuth 整合 (T041-T043) - 可與步驟 4 並行
4. 綁定頁面 (T044-T048) - 可與步驟 3 並行
5. 訪客限制 (T049-T050)

#### User Story 2 (推送通知)
1. 測試 (T051-T056) → 執行確認失敗 (T057)
2. 服務實作 (T058-T066) → 執行確認通過 (T066)
3. Token 安全 (T067-T070) - 可與步驟 4 並行
4. 整合流程 (T071-T073) - 依賴步驟 2
5. 配額監控 (T074-T077) - 可在步驟 4 之後

#### User Story 3 (LINE 端回報)
1. 對話管理測試 (T078-T085) → 執行確認失敗 (T086)
2. 對話服務實作 (T087-T098) → 執行確認通過 (T098)
3. Webhook 測試 (T099-T104) → 執行確認失敗 (T105)
4. Webhook 實作 (T106-T114) → 執行確認通過 (T114)
5. Middleware 與端點 (T115-T118) - 依賴步驟 4
6. 背景服務 (T119-T121) - 可與步驟 5 並行
7. 整合測試 (T122-T124,可選)

### 並行執行機會

#### Phase 2 (Foundational) 可並行任務組
```bash
# 組 1: 列舉類別 (T007)
# 組 2: 實體類別 (T008, T009, T010) - 可同時進行
# 組 3: EF Core 配置 (T012, T013, T014) - 等待組 2 完成
# 組 4: DTO (T019, T020, T021) - 可與組 3 並行
# 組 5: Extension Methods (T022, T023, T024) - 可與組 4 並行
# 組 6: 例外類別 (T025, T026) - 可與任何組並行
```

#### User Story 1 並行機會
```bash
# 所有測試 (T029-T035) 可同時撰寫
# OAuth 整合 (T041-T043) 與 綁定頁面 (T044-T048) 可並行
```

#### User Story 2 並行機會
```bash
# 所有測試 (T051-T056) 可同時撰寫
# Token 服務 (T067-T069) 與 配額監控 (T074-T076) 可並行
```

#### User Story 3 並行機會
```bash
# 對話管理測試 (T078-T085) 可同時撰寫
# Webhook 測試 (T099-T104) 可同時撰寫
# Middleware (T115-T118) 與 背景服務 (T119-T121) 可並行
```

#### Phase 7 (Polish) 可並行任務
```bash
# 組 1: 效能優化 (T131, T132, T134)
# 組 2: 安全性 (T135, T136, T137)
# 組 3: 文件更新 (T142, T143, T144, T145)
# 所有組別可完全並行執行
```

---

## 實作策略建議

### MVP First (最小可行產品)

**目標**: 最快速度交付核心價值

1. **完成 Phase 1 + Phase 2**: 建立基礎建設 (必須)
2. **完成 Phase 3 (US1)**: 實作 LINE 綁定功能
3. **停止並驗證**: 測試綁定功能是否正常運作
4. **部署/展示**: 此時已可展示「使用者可綁定 LINE 帳號」的基礎功能

**估計時間**: 22-28 小時 (Phase 1: 3h + Phase 2: 10h + Phase 3: 15h)

### 漸進式交付 (Incremental Delivery)

**目標**: 每個 User Story 完成後立即交付價值

1. **基礎 → US1**: Setup + Foundational + LINE 綁定 (MVP) → 測試 → 部署
2. **基礎 + US1 → US2**: 新增推送通知 → 測試 → 部署
3. **基礎 + US1 + US2 → US3**: 新增 LINE 端回報 → 測試 → 部署
4. **完整功能 → Polish**: 新增管理介面與最佳化 → 測試 → 最終部署

**每個階段都增加新價值且不破壞已有功能**

### 團隊並行策略 (多人協作)

**前提**: Phase 2 (Foundational) 必須完成

1. **階段 1**: 全員完成 Phase 1 + Phase 2 (共同建立基礎)
2. **階段 2**: Foundational 完成後,分工實作:
   - 開發者 A: User Story 1 (LINE 綁定)
   - 開發者 B: User Story 2 (推送通知) - 需等待 A 完成綁定服務介面
   - 開發者 C: User Story 3 (LINE 端回報) - 需等待 A 完成綁定服務,B 完成推送服務
3. **階段 3**: 各 User Story 完成後獨立測試與整合

**注意**: US2 與 US3 有對 US1 的依賴,建議 A 先完成 `ILineBindingService` 介面定義,B 與 C 可並行開發其他部分

---

## 總結

**總任務數**: 150 個任務

**任務分布**:
- Phase 1 (Setup): 6 個任務
- Phase 2 (Foundational): 22 個任務
- Phase 3 (US1): 22 個任務
- Phase 4 (US2): 27 個任務
- Phase 5 (US3): 47 個任務
- Phase 6 (管理介面): 6 個任務
- Phase 7 (Polish): 20 個任務

**估計總時間**:
- MVP (Phase 1 + 2 + 3): 22-28 小時
- 完整功能 (含 US1-US3): 60-75 小時
- 包含管理介面與 Polish: 70-90 小時

**並行機會**: 標記 [P] 的任務共 60+ 個,可大幅縮短總開發時間 (若有團隊協作)

**測試策略**: 所有服務均採用 TDD 方式開發,測試先行確保程式碼品質

**獨立交付**: 每個 User Story 完成後即可獨立測試與部署,支援漸進式交付

---

## 注意事項

1. **必須遵循順序**: Phase 2 (Foundational) 必須完成後才能開始任何 User Story
2. **測試先行**: 所有標記「撰寫單元測試」的任務必須在實作前完成,並確認測試失敗
3. **獨立驗證**: 每個 User Story 完成後必須執行 Checkpoint 驗證,確保功能正常
4. **Git 提交**: 每完成一個任務或邏輯組合後即提交,便於回溯與 Code Review
5. **安全性**: 絕對不要將 LINE 憑證提交到版本控制,僅使用 User Secrets 或環境變數
6. **文件同步**: 實作過程中若發現設計文件錯誤或需調整,立即更新對應文件

**需要協助?** 參考 quickstart.md 獲取詳細設定步驟,或查看 research.md 了解技術選型理由。
