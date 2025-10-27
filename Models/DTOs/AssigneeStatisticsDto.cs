namespace ClarityDesk.Models.DTOs;

/// <summary>
/// 處理人統計資料 DTO
/// </summary>
public class AssigneeStatisticsDto
{
    /// <summary>
    /// 處理人姓名
    /// </summary>
    public string AssigneeName { get; set; } = string.Empty;

    /// <summary>
    /// 被指派事件數量
    /// </summary>
    public int AssignedCount { get; set; }

    /// <summary>
    /// 處理中事件數量
    /// </summary>
    public int InProgressCount { get; set; }

    /// <summary>
    /// 已完成事件數量（即 Pending 數量）
    /// </summary>
    public int CompletedCount { get; set; }
}
