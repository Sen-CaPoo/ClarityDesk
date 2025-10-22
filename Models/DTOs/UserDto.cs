using ClarityDesk.Models.Enums;

namespace ClarityDesk.Models.DTOs;

/// <summary>
/// 使用者資料傳輸物件
/// </summary>
public class UserDto
{
    /// <summary>
    /// 使用者 ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// LINE User ID (唯一識別碼)
    /// </summary>
    public string LineUserId { get; set; } = string.Empty;

    /// <summary>
    /// 顯示名稱
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// 電子信箱
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// 權限角色
    /// </summary>
    public UserRole Role { get; set; }

    /// <summary>
    /// 帳號狀態
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// LINE 頭像 URL
    /// </summary>
    public string? PictureUrl { get; set; }

    /// <summary>
    /// 建立時間
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
