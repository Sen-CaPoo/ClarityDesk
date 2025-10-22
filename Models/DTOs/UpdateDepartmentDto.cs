using System.ComponentModel.DataAnnotations;

namespace ClarityDesk.Models.DTOs;

/// <summary>
/// 更新單位 DTO
/// </summary>
public class UpdateDepartmentDto
{
    /// <summary>
    /// 單位 ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 單位名稱
    /// </summary>
    [Required(ErrorMessage = "單位名稱為必填")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "單位名稱長度必須介於 2-100 個字元")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 單位說明
    /// </summary>
    [StringLength(500, ErrorMessage = "單位說明不可超過 500 個字元")]
    public string? Description { get; set; }

    /// <summary>
    /// 是否啟用
    /// </summary>
    public bool IsActive { get; set; } = true;
}
