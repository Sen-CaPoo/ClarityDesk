using ClarityDesk.Models.DTOs;

namespace ClarityDesk.Models.ViewModels;

/// <summary>
/// 回報單詳情頁面 ViewModel
/// </summary>
public class IssueReportViewModel
{
    /// <summary>
    /// 回報單資料
    /// </summary>
    public IssueReportDto IssueReport { get; set; } = new();

    /// <summary>
    /// 是否可編輯 (根據使用者權限決定)
    /// </summary>
    public bool CanEdit { get; set; }

    /// <summary>
    /// 是否可刪除 (根據使用者權限決定)
    /// </summary>
    public bool CanDelete { get; set; }

    /// <summary>
    /// 當前使用者 ID
    /// </summary>
    public int CurrentUserId { get; set; }

    /// <summary>
    /// 當前使用者是否為管理員
    /// </summary>
    public bool IsAdmin { get; set; }
}
