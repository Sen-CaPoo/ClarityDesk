# 研究報告: LINE 官方帳號整合技術選型

**Feature Branch**: `002-line-integration`  
**Research Date**: 2025-10-23  
**Status**: Complete

本文件記錄 LINE 整合功能的技術研究結果,解決 Technical Context 中標註的所有 NEEDS CLARIFICATION 項目。

---

## 1. LINE Messaging API 的 .NET SDK 選擇

### 決策: 使用 Line.Messaging NuGet 套件

### 理由:
- **官方支援**: `Line.Messaging` 是由 LINE Corporation 維護的官方 .NET SDK,版本穩定且持續更新
- **完整功能**: 支援 Messaging API、Webhook 事件處理、Flex Message 建構器、簽章驗證等核心功能
- **型別安全**: 提供強型別 C# 類別對應 LINE API 物件,減少手動 JSON 序列化錯誤
- **社群支援**: 有完善的文件與社群範例,降低開發風險

### 考慮的替代方案:
- **手動 HTTP 呼叫 (HttpClient)**: 
  - 優點: 完全控制、無第三方依賴
  - 缺點: 需手動處理 JSON 序列化、簽章驗證、錯誤處理,開發成本高且容易出錯
- **第三方社群套件 (isRock.LineBot)**: 
  - 優點: 台灣開發者維護,有繁體中文文件
  - 缺點: 非官方維護,長期支援性不確定

### 實作要點:
```csharp
// NuGet 套件安裝
// Install-Package Line.Messaging

// 服務註冊範例
services.AddSingleton<ILineMessagingClient>(sp => 
{
    var channelAccessToken = Configuration["LineSettings:ChannelAccessToken"];
    return new LineMessagingClient(channelAccessToken);
});
```

---

## 2. LINE Login OAuth 整合方式

### 決策: 使用 ASP.NET Core OAuth 中介軟體搭配自訂 LINE Provider

### 理由:
- **原生整合**: ASP.NET Core Identity 原生支援 OAuth 2.0 流程,僅需擴充新的 Provider
- **一致性**: 與現有 Google/Facebook 等第三方登入流程一致,降低學習成本
- **安全性**: 微軟提供的 OAuth 基礎設施經過嚴格安全審查,處理 Token 管理與 CSRF 防護
- **可維護性**: LINE API 變更時僅需更新 Provider 設定,不影響核心驗證邏輯

### 考慮的替代方案:
- **第三方套件 (AspNet.Security.OAuth.Line)**: 
  - 優點: 開箱即用,無需自行實作
  - 缺點: 需依賴第三方維護,可能與專案 ASP.NET Core 版本不相容
- **完全自訂 AuthenticationHandler**: 
  - 優點: 完全控制流程
  - 缺點: 需實作複雜的 OAuth 狀態管理、Token 驗證,開發成本過高

### 實作要點:
```csharp
// Program.cs 中設定 LINE OAuth
services.AddAuthentication()
    .AddOAuth("LINE", options =>
    {
        options.ClientId = Configuration["LineLogin:ChannelId"];
        options.ClientSecret = Configuration["LineLogin:ChannelSecret"];
        options.CallbackPath = new PathString("/signin-line");
        
        options.AuthorizationEndpoint = "https://access.line.me/oauth2/v2.1/authorize";
        options.TokenEndpoint = "https://api.line.me/oauth2/v2.1/token";
        options.UserInformationEndpoint = "https://api.line.me/v2/profile";
        
        options.Scope.Add("profile");
        options.Scope.Add("openid");
        
        options.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "userId");
        options.ClaimActions.MapJsonKey(ClaimTypes.Name, "displayName");
        
        options.Events = new OAuthEvents
        {
            OnCreatingTicket = async context =>
            {
                var lineUserId = context.Principal.FindFirstValue(ClaimTypes.NameIdentifier);
                var displayName = context.Principal.FindFirstValue(ClaimTypes.Name);
                
                // 呼叫 ILineBindingService 建立綁定關係
                var bindingService = context.HttpContext.RequestServices.GetRequiredService<ILineBindingService>();
                await bindingService.CreateOrUpdateBindingAsync(context.User.Id, lineUserId, displayName);
            }
        };
    });
```

---

## 3. LINE Webhook 簽章驗證實作模式

### 決策: 實作自訂 Middleware 進行簽章驗證

### 理由:
- **安全性優先**: 簽章驗證必須在請求到達 Controller 前完成,Middleware 是最適合的攔截點
- **關注點分離**: 驗證邏輯與業務邏輯分離,符合 Clean Architecture 原則
- **可重用性**: 相同的 Middleware 可用於所有 LINE Webhook 端點
- **效能考量**: 避免在每個 Controller Action 重複執行驗證邏輯

### 實作模式:
```csharp
// Infrastructure/Middleware/LineSignatureValidationMiddleware.cs
public class LineSignatureValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string _channelSecret;
    
    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/api/line/webhook"))
        {
            var signature = context.Request.Headers["X-Line-Signature"].FirstOrDefault();
            if (string.IsNullOrEmpty(signature))
            {
                context.Response.StatusCode = 401;
                return;
            }
            
            context.Request.EnableBuffering();
            using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
            var body = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;
            
            var computedSignature = ComputeHmacSha256(body, _channelSecret);
            if (signature != computedSignature)
            {
                context.Response.StatusCode = 401;
                return;
            }
        }
        
        await _next(context);
    }
    
    private string ComputeHmacSha256(string body, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(body));
        return Convert.ToBase64String(hash);
    }
}
```

### 替代方案:
- **Action Filter**: 在 Controller 層級驗證,但會增加每個 Action 的耦合性
- **使用 SDK 內建驗證**: Line.Messaging SDK 提供 `SignatureValidator`,可在 Controller 中使用

---

## 4. Flex Message JSON 結構設計最佳實務

### 決策: 使用 Line.Messaging SDK 的 FlexMessage Builder

### 理由:
- **型別安全**: Builder 模式避免手動撰寫 JSON 時的拼寫錯誤
- **可維護性**: 結構化的 C# 程式碼比複雜 JSON 更易閱讀與修改
- **驗證**: SDK 在建構時自動驗證結構合法性,避免執行時錯誤
- **範本管理**: 可將常用 Flex Message 封裝為可重用方法

### 實作範例 (推送通知訊息):
```csharp
public FlexMessage BuildIssueNotificationFlexMessage(IssueReportDto issue)
{
    var urgencyColor = issue.Urgency switch
    {
        Urgency.High => "#FF0000",    // 紅色
        Urgency.Medium => "#FFA500",  // 橙色
        Urgency.Low => "#00FF00"      // 綠色
    };
    
    var urgencyEmoji = issue.Urgency switch
    {
        Urgency.High => "🔴",
        Urgency.Medium => "🟡",
        Urgency.Low => "🟢"
    };
    
    return new FlexMessage("新回報單通知")
    {
        Contents = new BubbleContainer
        {
            Header = new BoxComponent
            {
                Layout = BoxLayout.Vertical,
                Contents = new IFlexComponent[]
                {
                    new TextComponent
                    {
                        Text = $"{urgencyEmoji} 新的問題回報",
                        Size = ComponentSize.Xl,
                        Weight = Weight.Bold,
                        Color = urgencyColor
                    }
                }
            },
            Body = new BoxComponent
            {
                Layout = BoxLayout.Vertical,
                Contents = new IFlexComponent[]
                {
                    CreateInfoRow("回報單編號", issue.IssueNumber),
                    CreateInfoRow("問題標題", issue.Title),
                    CreateInfoRow("所屬單位", issue.DepartmentName),
                    CreateInfoRow("聯絡人", issue.ContactName),
                    CreateInfoRow("連絡電話", issue.ContactPhone),
                    CreateInfoRow("紀錄日期", issue.RecordDate.ToString("yyyy/MM/dd HH:mm"))
                }
            },
            Footer = new BoxComponent
            {
                Layout = BoxLayout.Vertical,
                Contents = new IFlexComponent[]
                {
                    new ButtonComponent
                    {
                        Style = ButtonStyle.Primary,
                        Action = new UriAction
                        {
                            Label = "查看回報單詳情",
                            Uri = new Uri($"https://claritydesk.example.com/Issues/Details/{issue.Id}")
                        }
                    }
                }
            }
        }
    };
}

private BoxComponent CreateInfoRow(string label, string value)
{
    return new BoxComponent
    {
        Layout = BoxLayout.Horizontal,
        Contents = new IFlexComponent[]
        {
            new TextComponent
            {
                Text = label,
                Size = ComponentSize.Sm,
                Color = "#555555",
                Flex = 0,
                Weight = Weight.Bold
            },
            new TextComponent
            {
                Text = value,
                Size = ComponentSize.Sm,
                Color = "#111111",
                Wrap = true
            }
        },
        Margin = Spacing.Md
    };
}
```

### 替代方案:
- **直接使用 JSON 字串**: 靈活但容易出錯,不推薦用於複雜結構
- **JSON 檔案範本**: 適合固定格式訊息,但動態欄位處理複雜

---

## 5. LINE API 錯誤處理與重試策略

### 決策: 單次嘗試 + 結構化日誌記錄,不實作重試機制

### 理由:
- **需求明確**: 規格說明 (FR-015) 明確要求「不進行重試」
- **業務邏輯優先**: 推送通知失敗不應影響核心業務 (回報單建立),單次失敗可接受
- **避免浪費配額**: LINE API 有每月推送訊息限制,重試可能浪費配額
- **監控優先**: 透過日誌記錄失敗原因,管理員可手動處理或調整策略

### 實作模式:
```csharp
public async Task<bool> SendIssueNotificationAsync(string lineUserId, IssueReportDto issue, CancellationToken cancellationToken = default)
{
    try
    {
        _logger.LogInformation("開始發送回報單通知: IssueId={IssueId}, LineUserId={LineUserId}", issue.Id, lineUserId);
        
        var flexMessage = BuildIssueNotificationFlexMessage(issue);
        await _lineMessagingClient.PushMessageAsync(lineUserId, new[] { flexMessage }, cancellationToken);
        
        _logger.LogInformation("回報單通知發送成功: IssueId={IssueId}", issue.Id);
        return true;
    }
    catch (LineResponseException ex)
    {
        // LINE API 回應錯誤 (例如使用者封鎖、Channel Access Token 無效)
        _logger.LogError(ex, "LINE API 錯誤: IssueId={IssueId}, StatusCode={StatusCode}, Message={Message}", 
            issue.Id, ex.StatusCode, ex.Message);
        
        // 特定錯誤碼處理
        if (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
        {
            // 使用者封鎖官方帳號,標記綁定狀態
            await _lineBindingService.MarkAsBlockedAsync(lineUserId);
        }
        
        return false;
    }
    catch (Exception ex)
    {
        // 非預期錯誤 (網路問題、序列化錯誤等)
        _logger.LogError(ex, "推送通知發生未預期錯誤: IssueId={IssueId}", issue.Id);
        return false;
    }
}
```

### 監控指標:
- 每日推送成功率 (目標 ≥ 95%)
- 常見失敗原因分布 (封鎖、Token 錯誤、網路逾時)
- API 配額使用量 (每月 500 則限制)

### 未來可擴展方案:
若業務需求變更需要重試,可使用 Polly 套件實作:
```csharp
// 範例: 3 次重試,間隔 2 秒
var retryPolicy = Policy
    .Handle<LineResponseException>(ex => ex.StatusCode == System.Net.HttpStatusCode.InternalServerError)
    .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(2));
    
await retryPolicy.ExecuteAsync(async () => 
{
    await _lineMessagingClient.PushMessageAsync(lineUserId, messages);
});
```

---

## 6. Session 管理與逾時清理的背景任務實作

### 決策: 使用 .NET Hosted Service 搭配定期排程

### 理由:
- **原生支援**: ASP.NET Core 內建 `IHostedService` 與 `BackgroundService`,無需第三方依賴
- **輕量級**: 對於簡單的定期清理任務,Hosted Service 足夠且資源消耗低
- **生命週期管理**: 與應用程式生命週期整合,自動啟動與停止
- **避免過度工程**: Hangfire 等任務排程器適合複雜場景,本專案需求單純不需引入

### 實作模式:
```csharp
// Infrastructure/BackgroundServices/LineSessionCleanupService.cs
public class LineSessionCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<LineSessionCleanupService> _logger;
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(1); // 每小時執行一次
    
    public LineSessionCleanupService(IServiceProvider serviceProvider, ILogger<LineSessionCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("LINE Session 清理服務已啟動");
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_cleanupInterval, stoppingToken);
                
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                
                var expiredSessions = await dbContext.LineConversationSessions
                    .Where(s => s.ExpiresAt < DateTime.UtcNow)
                    .ToListAsync(stoppingToken);
                
                if (expiredSessions.Any())
                {
                    dbContext.LineConversationSessions.RemoveRange(expiredSessions);
                    await dbContext.SaveChangesAsync(stoppingToken);
                    
                    _logger.LogInformation("已清理 {Count} 個過期 Session", expiredSessions.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清理 Session 時發生錯誤");
                // 不中斷服務,繼續下一次清理
            }
        }
        
        _logger.LogInformation("LINE Session 清理服務已停止");
    }
}

// Program.cs 註冊
services.AddHostedService<LineSessionCleanupService>();
```

### Session 實體設計:
```csharp
public class LineConversationSession
{
    public Guid Id { get; set; }
    public string LineUserId { get; set; }
    public ConversationStep CurrentStep { get; set; }
    public string SessionData { get; set; } // JSON 格式暫存資料
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; } // CreatedAt + 30 分鐘
    public DateTime LastUpdatedAt { get; set; }
}
```

### 替代方案:
- **Hangfire**: 
  - 優點: 提供管理介面、分散式支援、失敗重試
  - 缺點: 需要額外資料表、增加專案複雜度,本專案規模不需要
- **Azure Timer Function**: 
  - 優點: 無伺服器架構,獨立於主應用程式
  - 缺點: 需要額外 Azure 資源與部署流程,增加營運成本

---

## 7. 電話號碼格式驗證規則

### 決策: 使用正規表示式驗證台灣手機號碼格式

### 驗證規則:
- **格式**: `09XX-XXXXXX` 或 `09XXXXXXXX` (總共 10 碼)
- **前綴**: 必須以 `09` 開頭
- **允許**: 數字與連字號 `-`

### 實作:
```csharp
// Models/Attributes/TaiwanPhoneNumberAttribute.cs
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class TaiwanPhoneNumberAttribute : ValidationAttribute
{
    private static readonly Regex PhoneRegex = new Regex(@"^09\d{2}-?\d{6}$", RegexOptions.Compiled);
    
    protected override ValidationResult IsValid(object value, ValidationContext validationContext)
    {
        if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
            return ValidationResult.Success; // 由 [Required] 處理必填驗證
        
        var phone = value.ToString().Replace("-", "");
        
        if (!PhoneRegex.IsMatch(phone))
            return new ValidationResult("請輸入有效的台灣手機號碼 (格式: 09XX-XXXXXX 或 09XXXXXXXX)");
        
        return ValidationResult.Success;
    }
}

// 使用範例
public class CreateIssueViewModel
{
    [Required(ErrorMessage = "請輸入連絡電話")]
    [TaiwanPhoneNumber]
    public string ContactPhone { get; set; }
}
```

### 客戶端驗證 (jQuery Validation):
```javascript
// wwwroot/js/validation-extensions.js
$.validator.addMethod("taiwanphone", function(value, element) {
    if (!value) return true; // 由 required 處理
    var phone = value.replace(/-/g, "");
    return /^09\d{8}$/.test(phone);
}, "請輸入有效的台灣手機號碼 (格式: 09XX-XXXXXX)");

$.validator.unobtrusive.adapters.add("taiwanphone", function(options) {
    options.rules["taiwanphone"] = true;
    if (options.message) {
        options.messages["taiwanphone"] = options.message;
    }
});
```

---

## 8. LINE API 配額監控機制

### 決策: 實作自訂 Metric 與警告通知機制

### 監控目標:
- **每月推送訊息數量**: 不超過 500 則 (免費方案限制)
- **警告閾值**: 達到 80% (400 則) 時發送警告
- **錯誤閾值**: 達到 95% (475 則) 時暫停推送並通知管理員

### 實作模式:
```csharp
// Services/LineUsageMonitorService.cs
public interface ILineUsageMonitorService
{
    Task<int> GetMonthlyPushCountAsync();
    Task<bool> CanSendPushMessageAsync();
    Task IncrementPushCountAsync();
}

public class LineUsageMonitorService : ILineUsageMonitorService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<LineUsageMonitorService> _logger;
    private readonly IEmailService _emailService; // 假設已有郵件服務
    private const int MonthlyLimit = 500;
    private const int WarningThreshold = 400;
    private const int ErrorThreshold = 475;
    
    public async Task<int> GetMonthlyPushCountAsync()
    {
        var currentMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        return await _dbContext.LineMessageLogs
            .Where(log => log.SentAt >= currentMonth && log.MessageType == "Push")
            .CountAsync();
    }
    
    public async Task<bool> CanSendPushMessageAsync()
    {
        var count = await GetMonthlyPushCountAsync();
        
        if (count >= ErrorThreshold)
        {
            _logger.LogWarning("LINE 推送訊息已達錯誤閾值: {Count}/{Limit}", count, MonthlyLimit);
            return false;
        }
        
        if (count >= WarningThreshold)
        {
            _logger.LogWarning("LINE 推送訊息已達警告閾值: {Count}/{Limit}", count, MonthlyLimit);
            await _emailService.SendWarningEmailAsync("LINE API 用量警告", 
                $"本月已使用 {count}/{MonthlyLimit} 則推送訊息");
        }
        
        return true;
    }
    
    public async Task IncrementPushCountAsync()
    {
        _dbContext.LineMessageLogs.Add(new LineMessageLog
        {
            MessageType = "Push",
            SentAt = DateTime.UtcNow
        });
        await _dbContext.SaveChangesAsync();
    }
}

// 整合至 LineMessagingService
public async Task<bool> SendIssueNotificationAsync(string lineUserId, IssueReportDto issue)
{
    // 檢查配額
    if (!await _usageMonitor.CanSendPushMessageAsync())
    {
        _logger.LogWarning("LINE 推送配額已用盡,跳過訊息發送: IssueId={IssueId}", issue.Id);
        return false;
    }
    
    // 發送訊息...
    var success = await SendMessageAsync(lineUserId, flexMessage);
    
    if (success)
    {
        await _usageMonitor.IncrementPushCountAsync();
    }
    
    return success;
}
```

### 資料表設計:
```csharp
public class LineMessageLog
{
    public Guid Id { get; set; }
    public string MessageType { get; set; } // "Push" or "Reply"
    public DateTime SentAt { get; set; }
    public bool IsSuccess { get; set; }
    public string ErrorMessage { get; set; }
}
```

---

## 9. 安全性考量: 快速連結 Token 機制

### 決策: 使用 Data Protection API 產生時效性 Token

### 理由:
- **原生整合**: ASP.NET Core Data Protection API 提供加密與時效性驗證
- **無需額外儲存**: Token 本身包含加密資訊,不需資料庫查詢
- **防止偽造**: 使用應用程式密鑰加密,無法被第三方偽造
- **自動過期**: 內建時效性檢查,避免長期連結安全風險

### 實作模式:
```csharp
// Services/IssueReportTokenService.cs
public interface IIssueReportTokenService
{
    string GenerateToken(int issueId, TimeSpan validity);
    int? ValidateToken(string token);
}

public class IssueReportTokenService : IIssueReportTokenService
{
    private readonly IDataProtector _protector;
    
    public IssueReportTokenService(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector("IssueReportAccess");
    }
    
    public string GenerateToken(int issueId, TimeSpan validity)
    {
        var expiration = DateTimeOffset.UtcNow.Add(validity);
        var payload = $"{issueId}|{expiration.ToUnixTimeSeconds()}";
        return _protector.Protect(payload);
    }
    
    public int? ValidateToken(string token)
    {
        try
        {
            var payload = _protector.Unprotect(token);
            var parts = payload.Split('|');
            
            var issueId = int.Parse(parts[0]);
            var expiration = DateTimeOffset.FromUnixTimeSeconds(long.Parse(parts[1]));
            
            if (DateTimeOffset.UtcNow > expiration)
                return null; // Token 已過期
            
            return issueId;
        }
        catch
        {
            return null; // Token 無效
        }
    }
}

// 產生 Flex Message 連結
var token = _tokenService.GenerateToken(issue.Id, TimeSpan.FromHours(24));
var url = $"https://claritydesk.example.com/Issues/Details/{issue.Id}?token={token}";

// 在 DetailsPage 驗證
public async Task<IActionResult> OnGetAsync(int id, string token = null)
{
    if (!User.Identity.IsAuthenticated && !string.IsNullOrEmpty(token))
    {
        var validatedIssueId = _tokenService.ValidateToken(token);
        if (validatedIssueId != id)
            return Unauthorized();
        
        // Token 有效,允許訪問
    }
    else if (!User.Identity.IsAuthenticated)
    {
        return RedirectToPage("/Account/Login");
    }
    
    // 載入回報單資料...
}
```

### 替代方案:
- **JWT Token**: 過於複雜,本場景不需要完整的身份驗證
- **資料庫儲存一次性連結**: 需要額外資料表與清理機制,增加複雜度

---

## 總結

所有技術選型已完成研究並記錄決策理由。主要技術堆疊:

| 技術領域 | 選擇 | 關鍵優勢 |
|---------|------|----------|
| LINE SDK | Line.Messaging (官方) | 型別安全、持續維護 |
| OAuth 整合 | ASP.NET Core OAuth Provider | 原生整合、安全性高 |
| 簽章驗證 | 自訂 Middleware | 關注點分離、可重用 |
| Flex Message | SDK Builder | 型別安全、易維護 |
| 錯誤處理 | 單次嘗試 + 日誌 | 符合需求、避免浪費配額 |
| Session 清理 | Hosted Service | 輕量級、原生支援 |
| Token 安全 | Data Protection API | 加密、自動過期 |

所有選擇均符合專案憲法的整潔架構、測試優先與效能標準要求。
