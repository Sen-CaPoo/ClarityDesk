namespace ClarityDesk.Models.DTOs;

/// <summary>
/// 回報人統計資料 DTO
/// </summary>
public class ReporterStatisticsDto
{
    /// <summary>
    /// 回報人姓名
    /// </summary>
    public string ReporterName { get; set; } = string.Empty;

    /// <summary>
    /// 回報事件數量
    /// </summary>
    public int IssueCount { get; set; }

    /// <summary>
    /// 待處理事件數量
    /// </summary>
    public int PendingCount { get; set; }

    /// <summary>
    /// 已解決事件數量
    /// </summary>
    public int ResolvedCount { get; set; }
}
