# Research: LINE 整合技術方案

**Date**: 2025-10-31
**Feature**: LINE 整合功能
**Status**: Completed

## 研究目標

解決 Technical Context 中的技術選型和實作細節：

1. LINE Messaging API SDK 選擇（官方或第三方）
2. Flex Message 格式設計與最佳實踐
3. 圖片附件儲存路徑策略
4. LINE Messaging API Rate Limits
5. 圖片附件大小限制
6. 對話狀態清理背景服務實作方式

## 研究結果

### 1. LINE Messaging API SDK 選擇

**決策**: 使用官方 **Line.Messaging** NuGet 套件（非官方但廣泛使用）或直接使用 HttpClient 實作

**理由**:

- **官方 SDK**: LINE 官方提供的是 Java、Python、PHP、Node.js、Go 的 SDK，目前沒有官方 .NET SDK
- **社群套件**: `Line.Messaging` (by Pierre-Luc Maheu) 是 .NET 社群最成熟的實作，但最後更新為 2020 年
- **推薦方案**: **使用 HttpClient 直接調用 LINE Messaging API**
  - 更輕量，無第三方依賴風險
  - LINE Messaging API 是標準 REST API，易於實作
  - 完全掌控請求/回應處理
  - 避免套件過時或不相容問題

**實作範例**:

```csharp
public class LineMessagingService : ILineMessagingService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public LineMessagingService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClient = httpClientFactory.CreateClient("LineMessagingAPI");
        _configuration = configuration;
    }

    public async Task<bool> PushMessageAsync(string userId, object message)
    {
        var channelAccessToken = _configuration["LineMessaging:ChannelAccessToken"];

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {channelAccessToken}");

        var payload = new
        {
            to = userId,
            messages = new[] { message }
        };

        var response = await _httpClient.PostAsJsonAsync(
            "https://api.line.me/v2/bot/message/push",
            payload);

        return response.IsSuccessStatusCode;
    }
}
```

**替代方案被拒絕原因**:

- `Line.Messaging` 套件：最後更新 2020 年，可能與 .NET 8 不相容，且不支援最新 LINE API 功能
- 自行包裝為獨立 NuGet：過度工程，專案規模不需要

### 2. Flex Message 格式設計與最佳實踐

**決策**: 使用 Bubble Container 結構，定義可重用的 C# 類別表示 Flex Message

**Flex Message 結構**:

LINE Flex Message 使用 JSON 格式定義，主要組件：

- **Bubble**: 單一訊息泡泡
- **Box**: 水平或垂直佈局容器
- **Text**: 文字元件
- **Button**: 按鈕元件（可開啟 URL 或觸發 Postback）
- **Image**: 圖片元件

**問題回報單通知 Flex Message 範例**:

```json
{
  "type": "flex",
  "altText": "新問題回報單通知",
  "contents": {
    "type": "bubble",
    "header": {
      "type": "box",
      "layout": "vertical",
      "contents": [
        {
          "type": "text",
          "text": "🔔 新問題回報單",
          "weight": "bold",
          "size": "lg",
          "color": "#1DB446"
        }
      ]
    },
    "body": {
      "type": "box",
      "layout": "vertical",
      "contents": [
        {
          "type": "box",
          "layout": "baseline",
          "contents": [
            {"type": "text", "text": "編號", "size": "sm", "color": "#999999", "flex": 2},
            {"type": "text", "text": "IR-12345", "size": "sm", "weight": "bold", "flex": 5}
          ]
        },
        {
          "type": "box",
          "layout": "baseline",
          "contents": [
            {"type": "text", "text": "標題", "size": "sm", "color": "#999999", "flex": 2},
            {"type": "text", "text": "系統登入問題", "size": "sm", "wrap": true, "flex": 5}
          ]
        },
        {
          "type": "box",
          "layout": "baseline",
          "contents": [
            {"type": "text", "text": "緊急程度", "size": "sm", "color": "#999999", "flex": 2},
            {"type": "text", "text": "🔴 高", "size": "sm", "color": "#FF0000", "flex": 5}
          ]
        },
        {
          "type": "separator",
          "margin": "md"
        },
        {
          "type": "box",
          "layout": "baseline",
          "contents": [
            {"type": "text", "text": "聯絡人", "size": "sm", "color": "#999999", "flex": 2},
            {"type": "text", "text": "張三", "size": "sm", "flex": 5}
          ]
        },
        {
          "type": "box",
          "layout": "baseline",
          "contents": [
            {"type": "text", "text": "電話", "size": "sm", "color": "#999999", "flex": 2},
            {"type": "text", "text": "0912345678", "size": "sm", "flex": 5}
          ]
        }
      ],
      "spacing": "sm"
    },
    "footer": {
      "type": "box",
      "layout": "vertical",
      "contents": [
        {
          "type": "button",
          "action": {
            "type": "uri",
            "label": "查看詳細內容",
            "uri": "https://claritydesk.example.com/Issues/Details/12345"
          },
          "style": "primary"
        }
      ]
    }
  }
}
```

**C# 類別設計**:

使用匿名物件或 DTO 類別構建 Flex Message：

```csharp
public class FlexMessageBuilder
{
    public static object BuildIssueNotification(IssueReportDto issue, string detailsUrl)
    {
        var priorityEmoji = issue.Priority switch
        {
            PriorityLevel.High => "🔴",
            PriorityLevel.Medium => "🟡",
            PriorityLevel.Low => "🟢",
            _ => ""
        };

        return new
        {
            type = "flex",
            altText = "新問題回報單通知",
            contents = new
            {
                type = "bubble",
                header = new
                {
                    type = "box",
                    layout = "vertical",
                    contents = new[]
                    {
                        new { type = "text", text = "🔔 新問題回報單", weight = "bold", size = "lg", color = "#1DB446" }
                    }
                },
                body = new
                {
                    type = "box",
                    layout = "vertical",
                    spacing = "sm",
                    contents = new object[]
                    {
                        CreateField("編號", $"IR-{issue.Id}"),
                        CreateField("標題", issue.Title, wrap: true),
                        CreateField("緊急程度", $"{priorityEmoji} {issue.Priority}"),
                        new { type = "separator", margin = "md" },
                        CreateField("聯絡人", issue.CustomerName),
                        CreateField("電話", issue.CustomerPhone)
                    }
                },
                footer = new
                {
                    type = "box",
                    layout = "vertical",
                    contents = new[]
                    {
                        new
                        {
                            type = "button",
                            action = new { type = "uri", label = "查看詳細內容", uri = detailsUrl },
                            style = "primary"
                        }
                    }
                }
            }
        };
    }

    private static object CreateField(string label, string value, bool wrap = false)
    {
        return new
        {
            type = "box",
            layout = "baseline",
            contents = new[]
            {
                new { type = "text", text = label, size = "sm", color = "#999999", flex = 2 },
                new { type = "text", text = value, size = "sm", wrap = wrap, flex = 5 }
            }
        };
    }
}
```

**最佳實踐**:

- 使用 `altText` 提供純文字替代方案（推播通知預覽）
- 限制 Bubble 高度（避免過長影響體驗）
- 使用顏色編碼表達狀態（綠色=低、黃色=中、紅色=高）
- Button URI 應為絕對 URL（HTTPS）

**替代方案被拒絕原因**:

- 純文字訊息：缺乏視覺吸引力，無法有效傳達結構化資訊
- Carousel 類型：單一問題通知不需多卡片輪播

### 3. 圖片附件儲存路徑策略

**決策**: 儲存於 `wwwroot/uploads/line-images/{IssueId}/{timestamp}_{filename}.jpg`

**理由**:

- **路徑結構**:
  - `wwwroot/uploads/` - 與現有專案靜態檔案管理一致
  - `line-images/` - 區分 LINE 來源的圖片
  - `{IssueId}/` - 依問題回報單 ID 分類，便於管理和刪除
  - `{timestamp}_{filename}.jpg` - 避免檔名衝突，保留原始檔名參考

- **配置方式**:

```json
// appsettings.json
{
  "LineMessaging": {
    "ChannelAccessToken": "YOUR_CHANNEL_ACCESS_TOKEN",
    "ChannelSecret": "YOUR_CHANNEL_SECRET",
    "ImageUploadPath": "wwwroot/uploads/line-images",
    "MaxImageSizeBytes": 10485760,  // 10 MB
    "MaxImagesPerIssue": 3
  }
}
```

- **清理策略**:
  - 選項 1：隨問題回報單刪除一併刪除（推薦，簡單）
  - 選項 2：定期清理超過 N 天的圖片（需背景服務）
  - 初期實作選項 1，後續可擴展選項 2

**實作範例**:

```csharp
public async Task<string> SaveLineImageAsync(int issueId, Stream imageStream, string originalFileName)
{
    var uploadPath = _configuration["LineMessaging:ImageUploadPath"];
    var issueDirectory = Path.Combine(uploadPath, issueId.ToString());

    Directory.CreateDirectory(issueDirectory);

    var timestamp = DateTime.UtcNow.Ticks;
    var fileName = $"{timestamp}_{Path.GetFileName(originalFileName)}";
    var filePath = Path.Combine(issueDirectory, fileName);

    using (var fileStream = new FileStream(filePath, FileMode.Create))
    {
        await imageStream.CopyToAsync(fileStream);
    }

    return $"/uploads/line-images/{issueId}/{fileName}";
}
```

**替代方案被拒絕原因**:

- 資料庫 BLOB 儲存：增加資料庫負擔，查詢效能差
- 雲端儲存（Azure Blob）：增加成本和複雜度，小規模專案不需要

### 4. LINE Messaging API Rate Limits

**研究結果**:

根據 LINE 官方文件，Push Message API 限制如下：

- **每秒請求數 (QPS)**: 無明確公開限制，建議保持 < 10 QPS
- **每日訊息數**: 取決於帳號類型
  - 免費方案：500 則/月
  - 輕量方案：~5000 則/月
  - 標準方案：無限制（但需付費）
- **Webhook 回應時間**: 必須在 3 秒內回應，否則 LINE 視為逾時並重試

**影響與對策**:

- **推送失敗處理**: 實作重試機制（最多 3 次，指數退避）
- **訊息量控制**: 僅關鍵事件推送（新增問題、狀態變更、指派變更）
- **監控**: 記錄推送失敗次數，達閾值時發送告警

**實作範例**:

```csharp
public async Task<bool> PushMessageWithRetryAsync(string userId, object message, int maxRetries = 3)
{
    for (int attempt = 0; attempt < maxRetries; attempt++)
    {
        try
        {
            var success = await PushMessageAsync(userId, message);
            if (success) return true;

            await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt))); // 1s, 2s, 4s
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "LINE Push Message 失敗 (嘗試 {Attempt}/{MaxRetries})", attempt + 1, maxRetries);
            if (attempt == maxRetries - 1) throw;
        }
    }

    return false;
}
```

**替代方案被拒絕原因**:

- 訊息佇列（RabbitMQ/Azure Service Bus）：過度工程，專案規模小不需要

### 5. 圖片附件大小限制

**研究結果**:

- **LINE 平台限制**:
  - 圖片格式：JPEG、PNG
  - 單張圖片大小：最大 10 MB
  - 解析度：最大 10000 x 10000 像素

- **專案限制設定**:
  - 單張圖片：最大 10 MB（與 LINE 一致）
  - 每個回報單最多 3 張圖片（需求規格）
  - ASP.NET Core 請求大小限制：預設 30 MB（需確認是否足夠）

**實作驗證**:

```csharp
public async Task<(bool IsValid, string ErrorMessage)> ValidateImageAsync(Stream imageStream, string fileName)
{
    var maxSize = _configuration.GetValue<long>("LineMessaging:MaxImageSizeBytes");

    if (imageStream.Length > maxSize)
    {
        return (false, $"圖片大小超過限制 ({maxSize / 1024 / 1024} MB)");
    }

    var extension = Path.GetExtension(fileName).ToLowerInvariant();
    if (extension != ".jpg" && extension != ".jpeg" && extension != ".png")
    {
        return (false, "僅支援 JPG 和 PNG 格式");
    }

    return (true, string.Empty);
}
```

**ASP.NET Core 配置**:

```csharp
// Program.cs
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 31457280; // 30 MB (3 images * 10 MB)
});
```

**替代方案被拒絕原因**:

- 自動壓縮圖片：增加伺服器負擔，且可能影響證據完整性（問題回報需原圖）

### 6. 對話狀態清理背景服務實作方式

**決策**: 使用 `IHostedService` 實作定期清理（初期可選，後續擴展）

**理由**:

- ASP.NET Core 內建 `IHostedService` 支援背景任務
- 使用 `BackgroundService` 基底類別簡化實作
- 每小時執行一次清理（檢查超過 24 小時的對話狀態）

**實作範例**:

```csharp
public class ConversationCleanupService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ConversationCleanupService> _logger;

    public ConversationCleanupService(
        IServiceScopeFactory scopeFactory,
        ILogger<ConversationCleanupService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupExpiredConversationsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清理對話狀態時發生錯誤");
            }

            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }

    private async Task CleanupExpiredConversationsAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var expirationTime = DateTime.UtcNow.AddHours(-24);
        var expiredStates = await dbContext.LineConversationStates
            .Where(s => s.CreatedAt < expirationTime)
            .ToListAsync();

        if (expiredStates.Any())
        {
            dbContext.LineConversationStates.RemoveRange(expiredStates);
            await dbContext.SaveChangesAsync();

            _logger.LogInformation("已清理 {Count} 筆過期對話狀態", expiredStates.Count);
        }
    }
}

// Program.cs 註冊
builder.Services.AddHostedService<ConversationCleanupService>();
```

**替代方案被拒絕原因**:

- Windows 排程工作（Task Scheduler）：需要額外設定，不如應用程式內建背景服務方便
- Azure Functions/AWS Lambda：增加成本和架構複雜度
- Hangfire/Quartz.NET：功能過於強大，本需求只需簡單定期任務

## 技術選型總結

| 項目 | 決策 | 主要依賴 |
|------|------|----------|
| LINE API SDK | HttpClient 直接調用 | `System.Net.Http.Json` (內建) |
| Flex Message | 匿名物件構建 JSON | 無額外依賴 |
| 圖片儲存 | 本地檔案系統 | `System.IO` (內建) |
| Rate Limit 處理 | 指數退避重試 | 無額外依賴 |
| 圖片驗證 | 自訂驗證邏輯 | 無額外依賴 |
| 背景清理 | `IHostedService` | `Microsoft.Extensions.Hosting` (內建) |

**新增 NuGet 套件**: **無** (完全使用 .NET 8 內建功能)

## 風險與緩解

| 風險 | 影響 | 緩解措施 |
|------|------|----------|
| LINE API 變更 | 高 | 使用 LINE 官方文件最新版本，監控 API 變更公告 |
| 推送失敗 | 中 | 實作重試機制，記錄失敗日誌，不影響回報單建立 |
| 圖片儲存空間 | 低 | 實作清理策略，監控磁碟使用量 |
| Rate Limit 超限 | 中 | 僅推送關鍵事件，監控推送頻率，考慮訊息合併 |
| Webhook 逾時 | 中 | 快速回應 200 OK，耗時處理改用背景工作 |

## 參考資料

- [LINE Messaging API 官方文件](https://developers.line.biz/en/docs/messaging-api/)
- [Flex Message 模擬器](https://developers.line.biz/flex-simulator/)
- [ASP.NET Core IHostedService 文件](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services)
