# API 合約定義: LINE 整合功能

**Feature Branch**: `002-line-integration`  
**Created**: 2025-10-23  
**Status**: Design Complete

本目錄包含 LINE 整合功能的所有服務介面、DTO (Data Transfer Objects) 與 API 端點定義。

---

## 目錄結構

```
contracts/
├── README.md                         # 本文件
├── services/                          # 服務介面定義
│   ├── ILineBindingService.md        # LINE 帳號綁定服務
│   ├── ILineMessagingService.md      # LINE 訊息發送服務
│   ├── ILineConversationService.md   # LINE 對話管理服務
│   └── ILineWebhookHandler.md        # LINE Webhook 事件處理
├── dtos/                              # DTO 定義
│   ├── LineBindingDtos.md            # 綁定相關 DTO
│   ├── LineMessageDtos.md            # 訊息相關 DTO
│   └── LineConversationDtos.md       # 對話相關 DTO
└── endpoints/                         # API 端點定義
    ├── LineWebhookEndpoints.md       # Webhook 端點
    └── LineManagementEndpoints.md    # 管理頁面端點
```

---

## 核心原則

### 1. 介面隔離原則 (Interface Segregation)
每個服務介面專注於單一職責:
- **ILineBindingService**: 僅處理綁定/解綁邏輯
- **ILineMessagingService**: 僅處理訊息發送
- **ILineConversationService**: 僅處理對話狀態管理
- **ILineWebhookHandler**: 僅處理 LINE 平台事件

### 2. DTO 設計原則
- **不可變性**: DTO 使用 `record` 類型或 `init` 屬性,確保不可變
- **驗證分離**: DTO 本身不包含驗證邏輯,使用 Data Annotations 或 FluentValidation
- **無業務邏輯**: DTO 僅作為資料容器,不包含業務邏輯方法
- **明確命名**: 使用 `*Request`, `*Response`, `*Dto` 後綴區分用途

### 3. 非同步優先
所有涉及 I/O 操作的方法必須為非同步 (`async Task<T>`) 並接受 `CancellationToken`。

### 4. 錯誤處理策略
- 預期錯誤: 回傳 `Result<T>` 或 `bool` + 輸出參數
- 非預期錯誤: 拋出自訂例外 (`LineApiException`, `LineBindingException`)
- 驗證錯誤: 回傳驗證結果物件 (`ValidationResult`)

---

## 命名慣例

### 服務介面
- 格式: `I{Domain}{Action}Service`
- 範例: `ILineBindingService`, `ILineMessagingService`

### DTO
- 請求 DTO: `{Action}{Entity}Request`
- 回應 DTO: `{Action}{Entity}Response`
- 資料傳輸 DTO: `{Entity}Dto`
- 範例: `CreateBindingRequest`, `LineBindingDto`, `SendMessageResponse`

### 方法命名
- 格式: `{Verb}{Object}Async`
- 範例: `CreateBindingAsync`, `SendPushMessageAsync`, `GetActiveSessionAsync`

---

## 依賴關係圖

```
┌─────────────────────────────────────┐
│    Razor Pages / Controllers        │
│   (Pages/Line/, Pages/Admin/Line/)  │
└─────────────────────────────────────┘
                  │
                  ▼
┌─────────────────────────────────────┐
│       Service Interfaces            │
│  (Services/Interfaces/ILine*.cs)    │
├─────────────────────────────────────┤
│ • ILineBindingService               │
│ • ILineMessagingService             │
│ • ILineConversationService          │
│ • ILineWebhookHandler               │
└─────────────────────────────────────┘
                  │
                  ▼
┌─────────────────────────────────────┐
│     Service Implementations         │
│      (Services/Line*.cs)            │
├─────────────────────────────────────┤
│ • LineBindingService                │
│ • LineMessagingService              │
│ • LineConversationService           │
│ • LineWebhookHandler                │
└─────────────────────────────────────┘
         │                    │
         ▼                    ▼
┌──────────────────┐  ┌──────────────────┐
│ ApplicationDbContext │  │ LINE Messaging   │
│   (EF Core)        │  │   API Client     │
└──────────────────┘  └──────────────────┘
```

---

## 外部依賴

### LINE Platform APIs
- **Messaging API**: `https://api.line.me/v2/bot/message/push`
- **OAuth API**: `https://api.line.me/oauth2/v2.1/token`
- **Profile API**: `https://api.line.me/v2/profile`

### NuGet Packages
- `Line.Messaging` (官方 SDK) - 主要依賴
- `Microsoft.Extensions.Options` - 組態管理
- `Microsoft.Extensions.Caching.Memory` - 快取

---

## 測試策略

### 單元測試覆蓋範圍
- ✅ 所有服務介面的公開方法
- ✅ DTO 驗證邏輯
- ✅ 錯誤處理路徑
- ✅ 邊界條件 (例如逾時 Session、重複綁定)

### 整合測試覆蓋範圍
- ✅ Webhook 端點 (使用模擬的 LINE 請求)
- ✅ 與資料庫互動 (使用 In-Memory Database)
- ✅ OAuth 流程 (使用 TestServer)

### 測試範例
```csharp
// 單元測試範例
[Fact]
public async Task CreateBindingAsync_DuplicateLineUserId_ThrowsLineBindingException()
{
    // Arrange
    var mockDbContext = CreateMockDbContext();
    var service = new LineBindingService(mockDbContext, _logger, _cache);
    
    // Act & Assert
    await Assert.ThrowsAsync<LineBindingException>(() =>
        service.CreateBindingAsync(userId: 1, lineUserId: "U123", displayName: "Test"));
}
```

---

## 版本管理

本合約定義遵循語意化版本控制:
- **MAJOR**: 破壞性變更 (例如移除方法、變更回傳型別)
- **MINOR**: 向後相容的新增功能 (例如新增選擇性參數)
- **PATCH**: 向後相容的錯誤修正

**當前版本**: 1.0.0 (初始版本)

---

## 詳細文件索引

請參考以下文件獲取完整的服務介面、DTO 與端點定義:

1. **服務介面**
   - [ILineBindingService](./services/ILineBindingService.md) - LINE 帳號綁定管理
   - [ILineMessagingService](./services/ILineMessagingService.md) - LINE 訊息發送與 Flex Message 建構
   - [ILineConversationService](./services/ILineConversationService.md) - LINE 對話 Session 管理
   - [ILineWebhookHandler](./services/ILineWebhookHandler.md) - LINE Webhook 事件處理

2. **DTO 定義**
   - [LineBindingDtos](./dtos/LineBindingDtos.md) - 綁定相關的請求/回應 DTO
   - [LineMessageDtos](./dtos/LineMessageDtos.md) - 訊息相關的 DTO 與 Flex Message 模型
   - [LineConversationDtos](./dtos/LineConversationDtos.md) - 對話狀態與 Session DTO

3. **API 端點**
   - [LineWebhookEndpoints](./endpoints/LineWebhookEndpoints.md) - LINE Webhook 端點規格
   - [LineManagementEndpoints](./endpoints/LineManagementEndpoints.md) - 綁定管理頁面端點規格
