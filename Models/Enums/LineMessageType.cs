namespace ClarityDesk.Models.Enums;

/// <summary>
/// LINE 訊息類型,影響配額計算
/// </summary>
public enum LineMessageType
{
    /// <summary>
    /// 主動推送訊息 (計入每月配額限制)
    /// </summary>
    Push,
    
    /// <summary>
    /// 回覆使用者訊息 (不計入配額,需在 Webhook 事件後 30 秒內回覆)
    /// </summary>
    Reply,
    
    /// <summary>
    /// 多播訊息 (向多個使用者推送相同訊息,計入配額)
    /// </summary>
    Multicast
}
