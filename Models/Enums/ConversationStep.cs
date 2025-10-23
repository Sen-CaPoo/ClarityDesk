namespace ClarityDesk.Models.Enums;

/// <summary>
/// LINE 對話流程的步驟狀態
/// </summary>
public enum ConversationStep
{
    /// <summary>
    /// 等待使用者輸入問題標題
    /// </summary>
    AwaitingTitle,
    
    /// <summary>
    /// 等待輸入問題內容描述
    /// </summary>
    AwaitingDescription,
    
    /// <summary>
    /// 等待選擇所屬單位 (透過快速回覆按鈕)
    /// </summary>
    AwaitingDepartment,
    
    /// <summary>
    /// 等待選擇緊急程度 (透過快速回覆按鈕)
    /// </summary>
    AwaitingUrgency,
    
    /// <summary>
    /// 等待輸入聯絡人姓名
    /// </summary>
    AwaitingContactName,
    
    /// <summary>
    /// 等待輸入連絡電話
    /// </summary>
    AwaitingContactPhone,
    
    /// <summary>
    /// 顯示摘要,等待確認或取消
    /// </summary>
    AwaitingConfirmation,
    
    /// <summary>
    /// 回報單已建立,Session 即將刪除
    /// </summary>
    Completed
}
