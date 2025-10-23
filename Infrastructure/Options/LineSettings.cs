namespace ClarityDesk.Infrastructure.Options;

/// <summary>
/// LINE 平台整合設定
/// </summary>
public class LineSettings
{
    /// <summary>
    /// LINE Channel ID
    /// </summary>
    public string ChannelId { get; set; } = string.Empty;

    /// <summary>
    /// LINE Channel Secret (用於簽章驗證)
    /// </summary>
    public string ChannelSecret { get; set; } = string.Empty;

    /// <summary>
    /// LINE Channel Access Token (用於 Messaging API)
    /// </summary>
    public string ChannelAccessToken { get; set; } = string.Empty;

    /// <summary>
    /// Webhook 回呼路徑 (預設: /api/line/webhook)
    /// </summary>
    public string WebhookPath { get; set; } = "/api/line/webhook";
    
    /// <summary>
    /// LINE Login OAuth 回呼路徑 (預設: /signin-line)
    /// </summary>
    public string CallbackPath { get; set; } = "/signin-line";
    
    /// <summary>
    /// Webhook URL (LINE Developers Console 設定用,完整 URL)
    /// </summary>
    public string WebhookUrl { get; set; } = string.Empty;

    /// <summary>
    /// 對話 Session 過期時間 (分鐘,預設: 30)
    /// </summary>
    public int SessionExpirationMinutes { get; set; } = 30;
    
    /// <summary>
    /// 每月推送訊息配額限制 (免費方案預設 500 則)
    /// </summary>
    public int MonthlyPushLimit { get; set; } = 500;
    
    /// <summary>
    /// 配額警告閾值 (百分比,預設: 80)
    /// </summary>
    public int QuotaWarningThresholdPercent { get; set; } = 80;
    
    /// <summary>
    /// 配額錯誤閾值 (百分比,預設: 95)
    /// </summary>
    public int QuotaErrorThresholdPercent { get; set; } = 95;
}
