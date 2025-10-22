using ClarityDesk.Models.Enums;

namespace ClarityDesk.Models.DTOs;

/// <summary>
/// 回報單篩選條件資料傳輸物件
/// </summary>
public class IssueFilterDto
{
    /// <summary>
    /// 處理狀態篩選
    /// </summary>
    public IssueStatus? Status { get; set; }

    /// <summary>
    /// 緊急程度篩選
    /// </summary>
    public PriorityLevel? Priority { get; set; }

    /// <summary>
    /// 問題所屬單位 ID 清單篩選
    /// </summary>
    public List<int>? DepartmentIds { get; set; }

    /// <summary>
    /// 指派處理人員 ID 清單篩選
    /// </summary>
    public List<int>? AssignedUserIds { get; set; }

    /// <summary>
    /// 開始日期篩選
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// 結束日期篩選
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// 搜尋關鍵字 (標題、內容、顧客名稱)
    /// </summary>
    public string? SearchKeyword { get; set; }

    /// <summary>
    /// 當前頁碼 (預設第 1 頁)
    /// </summary>
    public int CurrentPage { get; set; } = 1;

    /// <summary>
    /// 每頁筆數 (預設 20 筆)
    /// </summary>
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// 排序欄位 (預設 CreatedAt)
    /// </summary>
    public string SortBy { get; set; } = "CreatedAt";

    /// <summary>
    /// 排序方向 (預設降冪)
    /// </summary>
    public bool SortDescending { get; set; } = true;
}
