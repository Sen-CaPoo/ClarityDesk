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
    /// Webhook URL (LINE Developers Console 設定用)
    /// </summary>
    public string WebhookUrl { get; set; } = string.Empty;

    /// <summary>
    /// 每月推送訊息配額限制 (免費方案預設 500 則)
    /// </summary>
    public int MonthlyPushLimit { get; set; } = 500;
}
