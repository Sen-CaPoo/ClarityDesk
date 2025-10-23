namespace ClarityDesk.Models.Enums;

/// <summary>
/// 訊息傳送方向
/// </summary>
public enum MessageDirection
{
    /// <summary>
    /// 系統發送至 LINE 使用者 (Outbound)
    /// </summary>
    Outbound,
    
    /// <summary>
    /// LINE 使用者發送至系統 (Inbound)
    /// </summary>
    Inbound
}
