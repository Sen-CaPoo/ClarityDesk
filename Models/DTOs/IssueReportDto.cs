using ClarityDesk.Models.Enums;

namespace ClarityDesk.Models.DTOs;

/// <summary>
/// 回報單的顯示資料傳輸物件
/// </summary>
public class IssueReportDto
{
    /// <summary>
    /// 回報單 ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 問題標題
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 問題內容詳述
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 紀錄日期
    /// </summary>
    public DateTime RecordDate { get; set; }

    /// <summary>
    /// 處理狀態
    /// </summary>
    public IssueStatus Status { get; set; }

    /// <summary>
    /// 處理狀態顯示文字
    /// </summary>
    public string StatusText => Status switch
    {
        IssueStatus.Pending => "未處理",
        IssueStatus.Completed => "已處理",
        _ => "未知"
    };

    /// <summary>
    /// 緊急程度
    /// </summary>
    public PriorityLevel Priority { get; set; }

    /// <summary>
    /// 緊急程度顯示文字
    /// </summary>
    public string PriorityText => Priority switch
    {
        PriorityLevel.Low => "低",
        PriorityLevel.Medium => "中",
        PriorityLevel.High => "高",
        _ => "未知"
    };

    /// <summary>
    /// 回報人姓名
    /// </summary>
    public string ReporterName { get; set; } = string.Empty;

    /// <summary>
    /// 顧客聯絡人姓名
    /// </summary>
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>
    /// 顧客連絡電話
    /// </summary>
    public string CustomerPhone { get; set; } = string.Empty;

    /// <summary>
    /// 指派處理人員 ID
    /// </summary>
    public int AssignedUserId { get; set; }

    /// <summary>
    /// 指派處理人員名稱
    /// </summary>
    public string AssignedUserName { get; set; } = string.Empty;

    /// <summary>
    /// 問題所屬單位名稱清單
    /// </summary>
    public List<string> DepartmentNames { get; set; } = new();

    /// <summary>
    /// 問題所屬單位 ID 清單
    /// </summary>
    public List<int> DepartmentIds { get; set; } = new();

    /// <summary>
    /// 建立時間
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 最後修改時間
    /// </summary>
    public DateTime UpdatedAt { get; set; }
    
    /// <summary>
    /// 最後修改人 ID
    /// </summary>
    public int? LastModifiedByUserId { get; set; }
    
    /// <summary>
    /// 最後修改人名稱
    /// </summary>
    public string? LastModifiedByUserName { get; set; }
}
