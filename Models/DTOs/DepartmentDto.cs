namespace ClarityDesk.Models.DTOs;

/// <summary>
/// 單位 DTO
/// </summary>
public class DepartmentDto
{
    /// <summary>
    /// 單位 ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 單位名稱
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 單位說明
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 是否啟用
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// 建立時間
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 最後更新時間
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// 指派的處理人員清單
    /// </summary>
    public List<UserDto> AssignedUsers { get; set; } = new();
}
