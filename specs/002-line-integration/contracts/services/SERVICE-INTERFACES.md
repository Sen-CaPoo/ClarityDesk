# 服務介面摘要: LINE 整合功能

本文件提供所有 LINE 整合服務介面的完整定義,可直接作為實作參考。

---

## 1. ILineBindingService (LINE 帳號綁定服務)

### 職責
管理 ClarityDesk 使用者與 LINE 帳號的綁定關係,包含建立、查詢、解除綁定與狀態管理。

### 介面定義

```csharp
namespace ClarityDesk.Services.Interfaces
{
    /// <summary>
    /// LINE 帳號綁定服務介面
    /// </summary>
    public interface ILineBindingService
    {
        /// <summary>
        /// 建立或更新使用者的 LINE 綁定關係
        /// </summary>
        /// <param name="userId">ClarityDesk 使用者 ID</param>
        /// <param name="lineUserId">LINE User ID</param>
        /// <param name="displayName">LINE 顯示名稱</param>
        /// <param name="pictureUrl">LINE 頭像 URL (可選)</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>綁定記錄 ID</returns>
        /// <exception cref="LineBindingException">當 LINE User ID 已被其他帳號綁定時拋出</exception>
        /// <exception cref="InvalidOperationException">當使用者為訪客帳號時拋出</exception>
        Task<int> CreateOrUpdateBindingAsync(
            int userId,
            string lineUserId,
            string displayName,
            string? pictureUrl = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 取得使用者的 LINE 綁定資訊
        /// </summary>
        /// <param name="userId">ClarityDesk 使用者 ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>綁定資訊 DTO,若未綁定則回傳 null</returns>
        Task<LineBindingDto?> GetBindingByUserIdAsync(
            int userId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 依 LINE User ID 查詢綁定資訊
        /// </summary>
        /// <param name="lineUserId">LINE User ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>綁定資訊 DTO,若未找到則回傳 null</returns>
        Task<LineBindingDto?> GetBindingByLineUserIdAsync(
            string lineUserId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 檢查使用者是否已綁定 LINE 帳號
        /// </summary>
        /// <param name="userId">ClarityDesk 使用者 ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>true: 已綁定且狀態為 Active; false: 未綁定或狀態非 Active</returns>
        Task<bool> IsUserBoundAsync(
            int userId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 解除使用者的 LINE 綁定
        /// </summary>
        /// <param name="userId">ClarityDesk 使用者 ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>true: 成功解除; false: 使用者未綁定</returns>
        Task<bool> UnbindAsync(
            int userId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 標記綁定狀態為「已封鎖」(使用者封鎖 LINE 官方帳號)
        /// </summary>
        /// <param name="lineUserId">LINE User ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        Task MarkAsBlockedAsync(
            string lineUserId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 標記綁定狀態為「正常」(使用者解除封鎖或重新互動)
        /// </summary>
        /// <param name="lineUserId">LINE User ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        Task MarkAsActiveAsync(
            string lineUserId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 更新最後互動時間
        /// </summary>
        /// <param name="lineUserId">LINE User ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        Task UpdateLastInteractionAsync(
            string lineUserId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 取得所有已綁定的使用者列表 (管理員功能)
        /// </summary>
        /// <param name="status">篩選狀態 (可選,null 表示全部)</param>
        /// <param name="pageNumber">頁碼 (從 1 開始)</param>
        /// <param name="pageSize">每頁筆數</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>分頁結果</returns>
        Task<PagedResult<LineBindingDto>> GetAllBindingsAsync(
            BindingStatus? status = null,
            int pageNumber = 1,
            int pageSize = 20,
            CancellationToken cancellationToken = default);
    }
}
```

### DTO 定義

```csharp
namespace ClarityDesk.Models.DTOs
{
    /// <summary>
    /// LINE 綁定資訊 DTO
    /// </summary>
    public record LineBindingDto
    {
        public int Id { get; init; }
        public int UserId { get; init; }
        public string LineUserId { get; init; } = string.Empty;
        public string DisplayName { get; init; } = string.Empty;
        public string? PictureUrl { get; init; }
        public BindingStatus Status { get; init; }
        public DateTime BoundAt { get; init; }
        public DateTime LastInteractedAt { get; init; }
    }

    /// <summary>
    /// 綁定狀態列舉
    /// </summary>
    public enum BindingStatus
    {
        Active,    // 正常
        Blocked,   // 使用者封鎖官方帳號
        Unbound    // 已解除綁定
    }
}
```

---

## 2. ILineMessagingService (LINE 訊息發送服務)

### 職責
處理所有與 LINE Platform 的訊息發送互動,包含推送通知、回覆訊息與 Flex Message 建構。

### 介面定義

```csharp
namespace ClarityDesk.Services.Interfaces
{
    /// <summary>
    /// LINE 訊息發送服務介面
    /// </summary>
    public interface ILineMessagingService
    {
        /// <summary>
        /// 發送回報單通知給指派的處理人員
        /// </summary>
        /// <param name="lineUserId">目標 LINE 使用者 ID</param>
        /// <param name="issueReport">回報單資料 DTO</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>true: 發送成功; false: 發送失敗 (會記錄日誌)</returns>
        Task<bool> SendIssueNotificationAsync(
            string lineUserId,
            IssueReportDto issueReport,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 回覆使用者的訊息 (Reply API,不計入配額)
        /// </summary>
        /// <param name="replyToken">LINE Platform 提供的回覆令牌</param>
        /// <param name="messages">訊息內容 (最多 5 則)</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>true: 回覆成功; false: 回覆失敗</returns>
        Task<bool> ReplyMessageAsync(
            string replyToken,
            IEnumerable<string> messages,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 主動推送文字訊息給使用者
        /// </summary>
        /// <param name="lineUserId">目標 LINE 使用者 ID</param>
        /// <param name="message">訊息內容</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>true: 發送成功; false: 發送失敗</returns>
        Task<bool> PushTextMessageAsync(
            string lineUserId,
            string message,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 建構回報單通知的 Flex Message JSON
        /// </summary>
        /// <param name="issueReport">回報單資料 DTO</param>
        /// <returns>Flex Message JSON 字串</returns>
        string BuildIssueNotificationFlexMessage(IssueReportDto issueReport);

        /// <summary>
        /// 建構對話流程的快速回覆按鈕
        /// </summary>
        /// <param name="options">選項列表 (例如單位名稱、緊急程度)</param>
        /// <param name="maxOptions">最大選項數 (LINE 限制 13 個)</param>
        /// <returns>Quick Reply JSON</returns>
        string BuildQuickReplyButtons(
            IEnumerable<QuickReplyOption> options,
            int maxOptions = 13);

        /// <summary>
        /// 檢查是否可發送推送訊息 (配額檢查)
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>true: 可發送; false: 已達配額限制</returns>
        Task<bool> CanSendPushMessageAsync(
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 記錄訊息發送日誌
        /// </summary>
        /// <param name="log">訊息日誌 DTO</param>
        /// <param name="cancellationToken">取消令牌</param>
        Task LogMessageAsync(
            LineMessageLogDto log,
            CancellationToken cancellationToken = default);
    }
}
```

### DTO 定義

```csharp
namespace ClarityDesk.Models.DTOs
{
    /// <summary>
    /// 快速回覆選項
    /// </summary>
    public record QuickReplyOption(string Label, string Data);

    /// <summary>
    /// LINE 訊息日誌 DTO
    /// </summary>
    public record LineMessageLogDto
    {
        public string LineUserId { get; init; } = string.Empty;
        public LineMessageType MessageType { get; init; }
        public MessageDirection Direction { get; init; }
        public string Content { get; init; } = string.Empty; // JSON
        public bool IsSuccess { get; init; }
        public string? ErrorCode { get; init; }
        public string? ErrorMessage { get; init; }
        public int? IssueReportId { get; init; }
    }

    public enum LineMessageType
    {
        Push,       // 主動推送
        Reply,      // 回覆訊息
        Multicast   // 多播
    }

    public enum MessageDirection
    {
        Outbound,   // 系統發送
        Inbound     // 使用者傳入
    }
}
```

---

## 3. ILineConversationService (LINE 對話管理服務)

### 職責
管理 LINE 端回報問題的對話狀態,包含 Session 建立、步驟流轉、資料暫存與驗證。

### 介面定義

```csharp
namespace ClarityDesk.Services.Interfaces
{
    /// <summary>
    /// LINE 對話管理服務介面
    /// </summary>
    public interface ILineConversationService
    {
        /// <summary>
        /// 開始新的回報問題對話流程
        /// </summary>
        /// <param name="lineUserId">LINE 使用者 ID</param>
        /// <param name="userId">ClarityDesk 使用者 ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>Session ID</returns>
        /// <exception cref="InvalidOperationException">當使用者已有進行中的 Session 時拋出</exception>
        Task<Guid> StartConversationAsync(
            string lineUserId,
            int userId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 取得使用者當前的對話 Session
        /// </summary>
        /// <param name="lineUserId">LINE 使用者 ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>Session DTO,若無進行中 Session 則回傳 null</returns>
        Task<LineConversationSessionDto?> GetActiveSessionAsync(
            string lineUserId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 處理使用者輸入並推進對話流程
        /// </summary>
        /// <param name="lineUserId">LINE 使用者 ID</param>
        /// <param name="userInput">使用者輸入的文字</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>對話回應 (包含下一步提示與驗證結果)</returns>
        Task<ConversationResponse> ProcessUserInputAsync(
            string lineUserId,
            string userInput,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 更新 Session 的暫存資料
        /// </summary>
        /// <param name="sessionId">Session ID</param>
        /// <param name="fieldName">欄位名稱 (例如 "title", "departmentId")</param>
        /// <param name="value">欄位值</param>
        /// <param name="cancellationToken">取消令牌</param>
        Task UpdateSessionDataAsync(
            Guid sessionId,
            string fieldName,
            object value,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 推進對話至下一步驟
        /// </summary>
        /// <param name="sessionId">Session ID</param>
        /// <param name="nextStep">下一步驟</param>
        /// <param name="cancellationToken">取消令牌</param>
        Task AdvanceToNextStepAsync(
            Guid sessionId,
            ConversationStep nextStep,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 完成對話流程並建立回報單
        /// </summary>
        /// <param name="sessionId">Session ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>建立的回報單 ID</returns>
        Task<int> CompleteConversationAsync(
            Guid sessionId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 取消對話流程並清除 Session
        /// </summary>
        /// <param name="lineUserId">LINE 使用者 ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        Task CancelConversationAsync(
            string lineUserId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 驗證使用者輸入的資料格式
        /// </summary>
        /// <param name="step">當前步驟</param>
        /// <param name="input">使用者輸入</param>
        /// <returns>驗證結果</returns>
        ValidationResult ValidateInput(ConversationStep step, string input);

        /// <summary>
        /// 清理過期的 Session (由背景服務呼叫)
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>清理的 Session 數量</returns>
        Task<int> CleanupExpiredSessionsAsync(
            CancellationToken cancellationToken = default);
    }
}
```

### DTO 定義

```csharp
namespace ClarityDesk.Models.DTOs
{
    /// <summary>
    /// LINE 對話 Session DTO
    /// </summary>
    public record LineConversationSessionDto
    {
        public Guid Id { get; init; }
        public string LineUserId { get; init; } = string.Empty;
        public int UserId { get; init; }
        public ConversationStep CurrentStep { get; init; }
        public Dictionary<string, object> SessionData { get; init; } = new();
        public DateTime CreatedAt { get; init; }
        public DateTime ExpiresAt { get; init; }
    }

    /// <summary>
    /// 對話回應
    /// </summary>
    public record ConversationResponse
    {
        public bool IsValid { get; init; }
        public string Message { get; init; } = string.Empty;
        public ConversationStep? NextStep { get; init; }
        public IEnumerable<QuickReplyOption>? QuickReplyOptions { get; init; }
    }

    /// <summary>
    /// 對話步驟列舉
    /// </summary>
    public enum ConversationStep
    {
        AwaitingTitle,
        AwaitingDescription,
        AwaitingDepartment,
        AwaitingUrgency,
        AwaitingContactName,
        AwaitingContactPhone,
        AwaitingConfirmation,
        Completed
    }

    /// <summary>
    /// 驗證結果
    /// </summary>
    public record ValidationResult(bool IsValid, string? ErrorMessage = null);
}
```

---

## 4. ILineWebhookHandler (LINE Webhook 事件處理)

### 職責
處理來自 LINE Platform 的 Webhook 事件,分派至對應的處理邏輯。

### 介面定義

```csharp
namespace ClarityDesk.Services.Interfaces
{
    /// <summary>
    /// LINE Webhook 事件處理介面
    /// </summary>
    public interface ILineWebhookHandler
    {
        /// <summary>
        /// 處理 Webhook 事件 (主要入口方法)
        /// </summary>
        /// <param name="webhookPayload">LINE Platform 傳送的 JSON Payload</param>
        /// <param name="signature">X-Line-Signature 標頭值</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>HTTP 狀態碼 (200: 成功, 401: 簽章驗證失敗, 500: 處理錯誤)</returns>
        Task<int> HandleWebhookAsync(
            string webhookPayload,
            string signature,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 驗證 Webhook 請求的簽章
        /// </summary>
        /// <param name="payload">請求 Body (原始字串)</param>
        /// <param name="signature">X-Line-Signature 標頭值</param>
        /// <returns>true: 簽章有效; false: 簽章無效</returns>
        bool ValidateSignature(string payload, string signature);

        /// <summary>
        /// 處理 Follow 事件 (使用者加入好友)
        /// </summary>
        /// <param name="lineUserId">LINE 使用者 ID</param>
        /// <param name="replyToken">回覆令牌</param>
        /// <param name="cancellationToken">取消令牌</param>
        Task HandleFollowEventAsync(
            string lineUserId,
            string replyToken,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 處理 Unfollow 事件 (使用者封鎖或刪除好友)
        /// </summary>
        /// <param name="lineUserId">LINE 使用者 ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        Task HandleUnfollowEventAsync(
            string lineUserId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 處理 Message 事件 (使用者傳送訊息)
        /// </summary>
        /// <param name="lineUserId">LINE 使用者 ID</param>
        /// <param name="messageText">訊息內容</param>
        /// <param name="replyToken">回覆令牌</param>
        /// <param name="cancellationToken">取消令牌</param>
        Task HandleMessageEventAsync(
            string lineUserId,
            string messageText,
            string replyToken,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 處理 Postback 事件 (使用者點擊按鈕)
        /// </summary>
        /// <param name="lineUserId">LINE 使用者 ID</param>
        /// <param name="postbackData">按鈕攜帶的資料</param>
        /// <param name="replyToken">回覆令牌</param>
        /// <param name="cancellationToken">取消令牌</param>
        Task HandlePostbackEventAsync(
            string lineUserId,
            string postbackData,
            string replyToken,
            CancellationToken cancellationToken = default);
    }
}
```

---

## 共用型別

### 分頁結果

```csharp
namespace ClarityDesk.Models.DTOs
{
    /// <summary>
    /// 分頁結果包裝器
    /// </summary>
    public record PagedResult<T>
    {
        public IEnumerable<T> Items { get; init; } = Enumerable.Empty<T>();
        public int TotalCount { get; init; }
        public int PageNumber { get; init; }
        public int PageSize { get; init; }
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;
    }
}
```

### 自訂例外

```csharp
namespace ClarityDesk.Services.Exceptions
{
    /// <summary>
    /// LINE 綁定相關例外
    /// </summary>
    public class LineBindingException : Exception
    {
        public LineBindingException(string message) : base(message) { }
        public LineBindingException(string message, Exception innerException) 
            : base(message, innerException) { }
    }

    /// <summary>
    /// LINE API 呼叫例外
    /// </summary>
    public class LineApiException : Exception
    {
        public int? StatusCode { get; }
        public string? ErrorCode { get; }

        public LineApiException(string message, int? statusCode = null, string? errorCode = null) 
            : base(message)
        {
            StatusCode = statusCode;
            ErrorCode = errorCode;
        }
    }
}
```

---

## 服務註冊範例

```csharp
// Program.cs
services.AddScoped<ILineBindingService, LineBindingService>();
services.AddScoped<ILineMessagingService, LineMessagingService>();
services.AddScoped<ILineConversationService, LineConversationService>();
services.AddScoped<ILineWebhookHandler, LineWebhookHandler>();

// 註冊 LINE Messaging API Client
services.AddSingleton<ILineMessagingClient>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var channelAccessToken = configuration["LineSettings:ChannelAccessToken"];
    return new LineMessagingClient(channelAccessToken);
});

// 註冊背景服務
services.AddHostedService<LineSessionCleanupService>();
```

---

## 組態設定範例

```json
// appsettings.json
{
  "LineSettings": {
    "ChannelId": "YOUR_CHANNEL_ID",
    "ChannelSecret": "YOUR_CHANNEL_SECRET",
    "ChannelAccessToken": "YOUR_CHANNEL_ACCESS_TOKEN",
    "WebhookPath": "/api/line/webhook",
    "MonthlyPushLimit": 500,
    "SessionTimeoutMinutes": 30
  }
}
```

---

以上為所有核心服務介面的完整定義,可直接用於實作階段。
