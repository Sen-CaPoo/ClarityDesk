namespace ClarityDesk.Models.DTOs;

/// <summary>
/// 回報單統計資訊資料傳輸物件
/// </summary>
public class IssueStatisticsDto
{
    /// <summary>
    /// 回報單總數
    /// </summary>
    public int TotalIssues { get; set; }

    /// <summary>
    /// 未處理回報單數量
    /// </summary>
    public int PendingIssues { get; set; }

    /// <summary>
    /// 已處理回報單數量
    /// </summary>
    public int CompletedIssues { get; set; }

    /// <summary>
    /// 高優先級回報單數量
    /// </summary>
    public int HighPriorityIssues { get; set; }

    /// <summary>
    /// 本月新增回報單數量
    /// </summary>
    public int ThisMonthIssues { get; set; }

    /// <summary>
    /// 統計時間
    /// </summary>
    public DateTime StatisticsTime { get; set; } = DateTime.UtcNow;
}
