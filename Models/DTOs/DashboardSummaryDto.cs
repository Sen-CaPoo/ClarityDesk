namespace ClarityDesk.Models.DTOs;

/// <summary>
/// 儀表板綜合統計資訊 DTO
/// </summary>
public class DashboardSummaryDto
{
    /// <summary>
    /// 總回報單數量
    /// </summary>
    public int TotalIssues { get; set; }

    /// <summary>
    /// 未開始數量
    /// </summary>
    public int NotStartedCount { get; set; }

    /// <summary>
    /// 處理中數量
    /// </summary>
    public int InProgressCount { get; set; }

    /// <summary>
    /// 已完成數量
    /// </summary>
    public int CompletedCount { get; set; }

    /// <summary>
    /// 本月新增回報單數量
    /// </summary>
    public int ThisMonthNewIssues { get; set; }

    /// <summary>
    /// 本週新增回報單數量
    /// </summary>
    public int ThisWeekNewIssues { get; set; }
}
