using ClarityDesk.Models.Enums;

namespace ClarityDesk.Models.Entities;

/// <summary>
/// LINE 訊息發送日誌實體,記錄所有與 LINE Platform 的訊息互動
/// </summary>
public class LineMessageLog
{
    /// <summary>
    /// 主鍵,使用 GUID 格式
    /// </summary>
    public Guid Id { get; set; }
    
    /// <summary>
    /// 目標 LINE 使用者 ID
    /// </summary>
    public string LineUserId { get; set; } = string.Empty;
    
    /// <summary>
    /// 訊息類型
    /// </summary>
    public LineMessageType MessageType { get; set; }
    
    /// <summary>
    /// 訊息方向
    /// </summary>
    public MessageDirection Direction { get; set; }
    
    /// <summary>
    /// 訊息內容 (JSON 格式,包含完整訊息物件)
    /// </summary>
    public string Content { get; set; } = string.Empty;
    
    /// <summary>
    /// 發送是否成功
    /// </summary>
    public bool IsSuccess { get; set; }
    
    /// <summary>
    /// LINE API 錯誤代碼 (例如 401, 403)
    /// </summary>
    public string? ErrorCode { get; set; }
    
    /// <summary>
    /// 錯誤訊息描述
    /// </summary>
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// 發送時間 (UTC)
    /// </summary>
    public DateTime SentAt { get; set; }
    
    /// <summary>
    /// 關聯的回報單 ID (若訊息與特定回報單相關)
    /// </summary>
    public int? IssueReportId { get; set; }
    
    // 導覽屬性
    
    /// <summary>
    /// 關聯的回報單
    /// </summary>
    public IssueReport? IssueReport { get; set; }
}
