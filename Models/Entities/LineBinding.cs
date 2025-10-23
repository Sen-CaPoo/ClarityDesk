using ClarityDesk.Models.Enums;

namespace ClarityDesk.Models.Entities;

/// <summary>
/// LINE 帳號綁定實體,管理 ClarityDesk 使用者與 LINE 帳號的關聯
/// </summary>
public class LineBinding
{
    /// <summary>
    /// 主鍵ID
    /// </summary>
    public int Id { get; set; }
    
    /// <summary>
    /// ClarityDesk 使用者 ID
    /// </summary>
    public int UserId { get; set; }
    
    /// <summary>
    /// LINE Platform 使用者唯一識別碼 (格式: U + 32 個十六進位字元)
    /// </summary>
    public string LineUserId { get; set; } = string.Empty;
    
    /// <summary>
    /// LINE 使用者的顯示名稱
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;
    
    /// <summary>
    /// LINE 使用者的頭像 URL (可選)
    /// </summary>
    public string? PictureUrl { get; set; }
    
    /// <summary>
    /// 綁定狀態
    /// </summary>
    public BindingStatus BindingStatus { get; set; } = BindingStatus.Active;
    
    /// <summary>
    /// 首次綁定時間 (UTC)
    /// </summary>
    public DateTime BoundAt { get; set; }
    
    /// <summary>
    /// 最後一次與 LINE Bot 互動的時間 (用於活躍度分析)
    /// </summary>
    public DateTime LastInteractedAt { get; set; }
    
    /// <summary>
    /// 記錄建立時間
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// 記錄最後更新時間
    /// </summary>
    public DateTime UpdatedAt { get; set; }
    
    // 導覽屬性
    
    /// <summary>
    /// 關聯的 ClarityDesk 使用者
    /// </summary>
    public User? User { get; set; }
}
