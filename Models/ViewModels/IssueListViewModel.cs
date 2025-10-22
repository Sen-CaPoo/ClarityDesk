using ClarityDesk.Models.DTOs;
using ClarityDesk.Models.Entities;

namespace ClarityDesk.Models.ViewModels;

/// <summary>
/// 回報單列表頁面 ViewModel
/// </summary>
public class IssueListViewModel
{
    /// <summary>
    /// 分頁回報單資料
    /// </summary>
    public PagedResult<IssueReportDto> PagedIssues { get; set; } = new();

    /// <summary>
    /// 篩選條件
    /// </summary>
    public IssueFilterDto Filter { get; set; } = new();

    /// <summary>
    /// 統計資訊
    /// </summary>
    public IssueStatisticsDto Statistics { get; set; } = new();

    /// <summary>
    /// 可用的單位清單 (用於篩選)
    /// </summary>
    public List<Department> AvailableDepartments { get; set; } = new();

    /// <summary>
    /// 可用的處理人員清單 (用於篩選)
    /// </summary>
    public List<User> AvailableUsers { get; set; } = new();

    /// <summary>
    /// 當前使用者是否為管理員
    /// </summary>
    public bool IsAdmin { get; set; }
}
