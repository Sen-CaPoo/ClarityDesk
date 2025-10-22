using System.ComponentModel.DataAnnotations;
using ClarityDesk.Models.Enums;

namespace ClarityDesk.Models.DTOs;

/// <summary>
/// 建立回報單的資料傳輸物件
/// </summary>
public class CreateIssueReportDto
{
    /// <summary>
    /// 問題標題
    /// </summary>
    [Required(ErrorMessage = "標題為必填欄位")]
    [StringLength(30, MinimumLength = 1, ErrorMessage = "標題需在 1-30 字元之間")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 問題內容詳述
    /// </summary>
    [Required(ErrorMessage = "內容為必填欄位")]
    [StringLength(150, MinimumLength = 1, ErrorMessage = "內容需在 1-150 字元之間")]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 紀錄日期
    /// </summary>
    [Required(ErrorMessage = "紀錄日期為必填欄位")]
    [DataType(DataType.Date)]
    public DateTime RecordDate { get; set; }

    /// <summary>
    /// 處理狀態
    /// </summary>
    [Required(ErrorMessage = "處理狀態為必填欄位")]
    public IssueStatus Status { get; set; }

    /// <summary>
    /// 緊急程度
    /// </summary>
    [Required(ErrorMessage = "緊急程度為必填欄位")]
    public PriorityLevel Priority { get; set; }

    /// <summary>
    /// 回報人姓名
    /// </summary>
    [Required(ErrorMessage = "回報人姓名為必填欄位")]
    [StringLength(100, ErrorMessage = "回報人姓名不可超過 100 字元")]
    public string ReporterName { get; set; } = string.Empty;

    /// <summary>
    /// 顧客聯絡人姓名
    /// </summary>
    [Required(ErrorMessage = "顧客姓名為必填欄位")]
    [StringLength(100, ErrorMessage = "顧客姓名不可超過 100 字元")]
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>
    /// 顧客連絡電話
    /// </summary>
    [Required(ErrorMessage = "顧客電話為必填欄位")]
    [Phone(ErrorMessage = "請輸入有效的電話號碼")]
    [StringLength(20, ErrorMessage = "電話號碼不可超過 20 字元")]
    public string CustomerPhone { get; set; } = string.Empty;

    /// <summary>
    /// 指派處理人員 ID
    /// </summary>
    [Required(ErrorMessage = "指派處理人員為必填欄位")]
    public int AssignedUserId { get; set; }

    /// <summary>
    /// 問題所屬單位 ID 清單
    /// </summary>
    [Required(ErrorMessage = "問題所屬單位為必填欄位")]
    [MinLength(1, ErrorMessage = "至少需選擇一個單位")]
    public List<int> DepartmentIds { get; set; } = new();
}
