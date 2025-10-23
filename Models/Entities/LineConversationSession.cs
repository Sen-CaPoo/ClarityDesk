using ClarityDesk.Models.Enums;

namespace ClarityDesk.Models.Entities;

/// <summary>
/// LINE 對話 Session 實體,持久化儲存使用者在 LINE 端進行回報問題時的對話狀態
/// </summary>
public class LineConversationSession
{
    /// <summary>
    /// 主鍵,使用 GUID 格式
    /// </summary>
    public Guid Id { get; set; }
    
    /// <summary>
    /// LINE 使用者 ID
    /// </summary>
    public string LineUserId { get; set; } = string.Empty;
    
    /// <summary>
    /// ClarityDesk 使用者 ID
    /// </summary>
    public int UserId { get; set; }
    
    /// <summary>
    /// 當前對話步驟
    /// </summary>
    public ConversationStep CurrentStep { get; set; } = ConversationStep.AwaitingTitle;
    
    /// <summary>
    /// JSON 格式的暫存資料 (問題標題、內容、單位等)
    /// </summary>
    public string SessionData { get; set; } = "{}";
    
    /// <summary>
    /// Session 建立時間
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// 最後更新時間
    /// </summary>
    public DateTime UpdatedAt { get; set; }
    
    /// <summary>
    /// 過期時間,超過此時間自動清理 (CreatedAt + 30 分鐘)
    /// </summary>
    public DateTime ExpiresAt { get; set; }
    
    // 導覽屬性
    
    /// <summary>
    /// 關聯的 ClarityDesk 使用者
    /// </summary>
    public User? User { get; set; }
}
