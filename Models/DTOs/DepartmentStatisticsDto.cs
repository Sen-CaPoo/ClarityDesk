namespace ClarityDesk.Models.DTOs;

/// <summary>
/// 問題所屬單位統計資料 DTO
/// </summary>
public class DepartmentStatisticsDto
{
    /// <summary>
    /// 單位名稱
    /// </summary>
    public string DepartmentName { get; set; } = string.Empty;

    /// <summary>
    /// 事件數量
    /// </summary>
    public int IssueCount { get; set; }

    /// <summary>
    /// 未開始事件數量
    /// </summary>
    public int NotStartedCount { get; set; }

    /// <summary>
    /// 處理中事件數量
    /// </summary>
    public int InProgressCount { get; set; }

    /// <summary>
    /// 已完成事件數量
    /// </summary>
    public int CompletedCount { get; set; }
}
