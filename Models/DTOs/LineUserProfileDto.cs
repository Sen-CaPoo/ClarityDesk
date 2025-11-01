using System.Text.Json.Serialization;

namespace ClarityDesk.Models.DTOs;

/// <summary>
/// LINE API 回傳的使用者資料
/// </summary>
public class LineUserProfileDto
{
    /// <summary>
    /// LINE User ID
    /// </summary>
    [JsonPropertyName("userId")]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// 顯示名稱
    /// </summary>
    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// 頭像 URL
    /// </summary>
    [JsonPropertyName("pictureUrl")]
    public string? PictureUrl { get; set; }

    /// <summary>
    /// 狀態訊息
    /// </summary>
    [JsonPropertyName("statusMessage")]
    public string? StatusMessage { get; set; }
}
