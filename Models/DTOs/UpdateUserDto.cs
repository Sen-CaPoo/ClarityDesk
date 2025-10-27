using System.ComponentModel.DataAnnotations;

namespace ClarityDesk.Models.DTOs;

/// <summary>
/// 更新使用者資料傳輸物件
/// </summary>
public class UpdateUserDto
{
    /// <summary>
    /// 顯示名稱
    /// </summary>
    [Required(ErrorMessage = "顯示名稱為必填欄位")]
    [StringLength(100, ErrorMessage = "顯示名稱長度不可超過 100 個字元")]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// 電子信箱
    /// </summary>
    [EmailAddress(ErrorMessage = "請輸入有效的電子信箱格式")]
    [StringLength(200, ErrorMessage = "電子信箱長度不可超過 200 個字元")]
    public string? Email { get; set; }
}
