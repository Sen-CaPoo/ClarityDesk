namespace ClarityDesk.Infrastructure.Authentication;

/// <summary>
/// LINE Login OAuth 設定選項
/// </summary>
public class LineLoginOptions
{
    /// <summary>
    /// LINE Channel ID
    /// </summary>
    public string ChannelId { get; set; } = string.Empty;

    /// <summary>
    /// LINE Channel Secret
    /// </summary>
    public string ChannelSecret { get; set; } = string.Empty;

    /// <summary>
    /// OAuth Callback 路徑
    /// </summary>
    public string CallbackPath { get; set; } = "/signin-line";
}
