namespace ClarityDesk.Models.DTOs;

/// <summary>
/// LINE API 回傳的使用者資料
/// </summary>
public class LineUserProfileDto
{
    /// <summary>
    /// LINE User ID
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// 顯示名稱
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// 頭像 URL
    /// </summary>
    public string? PictureUrl { get; set; }

    /// <summary>
    /// 狀態訊息
    /// </summary>
    public string? StatusMessage { get; set; }
}
