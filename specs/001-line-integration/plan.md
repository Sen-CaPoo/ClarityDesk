# Implementation Plan: LINE 整合功能

**Branch**: `001-line-integration` | **Date**: 2025-10-31 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/001-line-integration/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

本功能實現 ClarityDesk 系統與 LINE Messaging API 的整合，提供三大核心功能：

1. **LINE 官方帳號綁定** - 透過 OAuth 流程建立系統帳號與 LINE 帳號的關聯
2. **問題回報單推送通知** - 使用 Flex Message 格式向已綁定使用者推送問題更新通知
3. **LINE 端問題回報** - 透過對話式介面在 LINE 中直接建立問題回報單

技術實現採用 LINE Messaging API v2，透過 Webhook 接收使用者訊息，使用 Push Message API 發送通知。對話狀態管理使用資料庫儲存，圖片附件儲存於本地檔案系統。遵循現有架構模式（Service Layer + EF Core），不引入 AutoMapper 或 Redis。

## Technical Context

**Language/Version**: C# / .NET 8.0

**Primary Dependencies**:

- ASP.NET Core 8.0 (Razor Pages)
- Entity Framework Core 8.0
- LINE Messaging API SDK (需研究選擇官方或第三方套件)
- EPPlus 7.0.5 (已安裝，用於 Excel 匯出)

**Storage**:

- SQL Server (主要資料庫，透過 EF Core)
- 本地檔案系統 (LINE 圖片附件暫存，需確認路徑策略)

**Testing**:

- xUnit (單元測試)
- Moq (模擬依賴)
- FluentAssertions (斷言)
- EF Core InMemory (測試資料庫)

**Target Platform**:

- Windows Server / IIS (生產環境)
- HTTPS required (LINE Webhook 必須使用 HTTPS)

**Project Type**: Web application (Razor Pages)

**Performance Goals**:

- LINE Webhook 回應時間 < 3 秒（LINE 平台要求）
- Push Message 發送延遲 < 10 秒
- 對話狀態查詢 < 100ms

**Constraints**:

- LINE Messaging API Rate Limits (需研究具體限制)
- 圖片附件大小限制 (LINE 平台限制需確認)
- Webhook 必須公開可訪問且使用 HTTPS
- 不使用 Redis（改用資料庫儲存對話狀態）
- 不使用 AutoMapper（使用 POCO 手動映射）

**Scale/Scope**:

- 預期同時線上使用者：< 100
- 每日 LINE 訊息量：< 1000
- 對話狀態保留：24 小時
- 圖片附件保留：需確認策略（與回報單生命週期一致或定期清理）

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### 評估結果：✅ 通過

本功能符合棕地專案憲章的核心原則：

#### 一、尊重棕地專案

- ✅ **新增功能，非重構**：本功能是全新的 LINE 整合模組，不修改現有正常運作的程式碼
- ✅ **遵循現有架構**：採用現有的 Service Layer + EF Core + Razor Pages 架構模式
- ✅ **保留現有程式碼風格**：使用與現有程式碼一致的 C# 慣例和命名規範
- ✅ **不影響現有功能**：LINE 整合為獨立模組，現有問題回報流程完全不受影響

#### 二、最小化變更原則

- ✅ **僅新增必要檔案**：
  - 新增實體：`LineBinding.cs`, `LinePushLog.cs`, `LineConversationState.cs`
  - 新增服務：`ILineMessagingService.cs`, `LineMessagingService.cs`
  - 新增 Razor Pages：`/Pages/Account/LineBinding.cshtml` 等
  - 新增 API Controller：`/Controllers/LineWebhookController.cs`
- ✅ **最小程度修改現有檔案**：
  - `Program.cs`：僅新增 LINE Messaging Service 註冊
  - `appsettings.json`：僅新增 LINE Messaging API 設定區塊
  - `ApplicationDbContext.cs`：僅新增新實體的 DbSet
- ✅ **不觸碰不相關檔案**：現有 IssueReportService、UserManagementService 等保持不變

#### 三、需要明確許可

- ✅ **新增 NuGet 套件需確認**：
  - 計畫新增：LINE Messaging API SDK (待研究選擇官方或社群套件)
  - 使用者已明確表示「現有的套件都不要更動」
- ✅ **架構決策已明確**：
  - 使用者明確要求「不使用 AutoMapper」、「不使用 Redis」
  - 圖片儲存使用本地檔案系統（符合現有架構）

#### 四、測試紀律

- ✅ **新增測試，不修改現有測試**：
  - 在 `Tests/ClarityDesk.UnitTests/` 新增 `LineMessagingServiceTests.cs`
  - 在 `Tests/ClarityDesk.IntegrationTests/` 新增 LINE Webhook 整合測試
  - 現有測試檔案完全不動
- ✅ **確保現有測試持續通過**：新增功能不影響現有業務邏輯，現有測試應保持綠燈

### 風險評估

**低風險變更**：

- 新增資料表不影響現有資料結構
- LINE 推送失敗不影響回報單建立（Fail-safe 設計）
- Webhook 端點為獨立 Controller，不與現有 Razor Pages 衝突

**需注意事項**：

- 圖片附件儲存路徑需配置在 `appsettings.json`，避免硬編碼
- LINE Messaging API 金鑰需使用 User Secrets 或環境變數（生產環境）
- 對話狀態清理需實作背景服務（BackgroundService），但可作為後續優化，初期可手動執行或排程

### Phase 1 設計後重新評估：✅ 持續符合

Phase 1 設計完成後，重新檢視憲章符合度：

#### 技術選型確認

- ✅ **無新增 NuGet 套件**：完全使用 .NET 8 內建 API（HttpClient、System.Text.Json）
- ✅ **不使用 AutoMapper**：所有 DTO 轉換使用手動映射（POCO）
- ✅ **不使用 Redis**：對話狀態儲存於 SQL Server
- ✅ **遵循現有架構**：Service Layer 模式、Fluent API 配置、xUnit 測試

#### 新增檔案清單（最小化原則）

**實體與配置** (7 個新檔案):

- `Models/Entities/LineBinding.cs`
- `Models/Entities/LinePushLog.cs`
- `Models/Entities/LineConversationState.cs`
- `Data/Configurations/LineBindingConfiguration.cs`
- `Data/Configurations/LinePushLogConfiguration.cs`
- `Data/Configurations/LineConversationStateConfiguration.cs`
- `Migrations/[timestamp]_AddLineTables.cs`

**DTOs 與 Enums** (6 個新檔案):

- `Models/DTOs/LineBindingDto.cs`
- `Models/DTOs/LinePushLogDto.cs`
- `Models/DTOs/LineMessageDto.cs`
- `Models/DTOs/FlexMessageDto.cs`
- `Models/Enums/LinePushStatus.cs`
- `Models/Enums/ConversationStep.cs`

**服務層** (2 個新檔案):

- `Services/Interfaces/ILineMessagingService.cs`
- `Services/LineMessagingService.cs`

**控制器與頁面** (3 個新檔案):

- `Controllers/LineWebhookController.cs`
- `Pages/Account/LineBinding.cshtml`
- `Pages/Account/LineBinding.cshtml.cs`

**背景服務（可選）** (1 個新檔案):

- `Infrastructure/BackgroundServices/ConversationCleanupService.cs`

**測試** (2 個新檔案):

- `Tests/ClarityDesk.UnitTests/LineMessagingServiceTests.cs`
- `Tests/ClarityDesk.IntegrationTests/LineWebhookIntegrationTests.cs`

**最小修改現有檔案** (3 個檔案):

- `Program.cs`：僅新增 1 行服務註冊 + 1 行 HttpClient 配置
- `appsettings.json`：僅新增 `LineMessaging` 設定區塊
- `Data/ApplicationDbContext.cs`：僅新增 3 個 DbSet 屬性

**總計**：21 個新檔案 + 3 個最小修改 = **符合最小化變更原則**

## Project Structure

### Documentation (this feature)

```text
specs/001-line-integration/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
│   ├── webhook-api.md   # LINE Webhook 接收訊息的 API 規格
│   ├── push-message.md  # LINE Push Message 發送規格
│   └── flex-message-templates.json  # Flex Message 範本定義
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
ClarityDesk/
├── Models/
│   ├── Entities/
│   │   ├── LineBinding.cs              # 新增：LINE 綁定記錄
│   │   ├── LinePushLog.cs              # 新增：LINE 推送記錄
│   │   └── LineConversationState.cs    # 新增：LINE 對話狀態
│   ├── DTOs/
│   │   ├── LineBindingDto.cs           # 新增：LINE 綁定 DTO
│   │   ├── LinePushLogDto.cs           # 新增：LINE 推送記錄 DTO
│   │   ├── LineMessageDto.cs           # 新增：LINE 訊息 DTO (Webhook)
│   │   └── FlexMessageDto.cs           # 新增：Flex Message DTO
│   └── Enums/
│       ├── LinePushStatus.cs           # 新增：推送狀態枚舉
│       └── ConversationStep.cs         # 新增：對話步驟枚舉
├── Services/
│   ├── Interfaces/
│   │   └── ILineMessagingService.cs    # 新增：LINE Messaging 服務介面
│   └── LineMessagingService.cs         # 新增：LINE Messaging 服務實作
├── Controllers/                         # 新增：API Controllers 目錄
│   └── LineWebhookController.cs        # 新增：LINE Webhook 控制器
├── Pages/
│   └── Account/
│       ├── LineBinding.cshtml          # 新增：LINE 綁定頁面
│       └── LineBinding.cshtml.cs       # 新增：LINE 綁定頁面邏輯
├── Data/
│   ├── ApplicationDbContext.cs         # 修改：新增 LINE 相關 DbSet
│   └── Configurations/
│       ├── LineBindingConfiguration.cs # 新增：LineBinding 實體配置
│       ├── LinePushLogConfiguration.cs # 新增：LinePushLog 實體配置
│       └── LineConversationStateConfiguration.cs # 新增：LineConversationState 實體配置
├── Infrastructure/
│   └── BackgroundServices/             # 新增：背景服務目錄
│       └── ConversationCleanupService.cs # 新增：對話狀態清理服務 (可選)
├── Migrations/
│   └── [timestamp]_AddLineTables.cs    # 新增：LINE 功能資料表遷移
├── wwwroot/
│   └── uploads/                        # 新增：圖片附件上傳目錄
│       └── line-images/                # 新增：LINE 圖片附件子目錄
├── Program.cs                          # 修改：註冊 LINE Messaging Service
└── appsettings.json                    # 修改：新增 LINE Messaging API 設定

Tests/
├── ClarityDesk.UnitTests/
│   └── LineMessagingServiceTests.cs    # 新增：LINE Messaging 服務單元測試
└── ClarityDesk.IntegrationTests/
    └── LineWebhookIntegrationTests.cs  # 新增：LINE Webhook 整合測試
```

**Structure Decision**: 採用現有的 Web Application (Razor Pages) 架構，並新增 API Controllers 目錄以處理 LINE Webhook。LINE 相關功能完全獨立於現有模組，透過服務層介面與問題回報系統整合。圖片附件儲存在 `wwwroot/uploads/line-images/` 目錄，遵循現有的靜態資源管理模式。

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| [e.g., 4th project] | [current need] | [why 3 projects insufficient] |
| [e.g., Repository pattern] | [specific problem] | [why direct DB access insufficient] |
