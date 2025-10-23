namespace ClarityDesk.Models.Enums;

/// <summary>
/// LINE 帳號綁定狀態
/// </summary>
public enum BindingStatus
{
    /// <summary>
    /// 正常啟用狀態,可接收推送訊息
    /// </summary>
    Active,
    
    /// <summary>
    /// 使用者封鎖官方帳號,無法發送訊息
    /// </summary>
    Blocked,
    
    /// <summary>
    /// 已解除綁定
    /// </summary>
    Unbound
}
