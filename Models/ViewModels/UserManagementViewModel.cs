using ClarityDesk.Models.DTOs;

namespace ClarityDesk.Models.ViewModels;

/// <summary>
/// 使用者管理 ViewModel
/// </summary>
public class UserManagementViewModel
{
    /// <summary>
    /// 使用者清單
    /// </summary>
    public List<UserDto> Users { get; set; } = new();

    /// <summary>
    /// 是否包含停用使用者
    /// </summary>
    public bool IncludeInactive { get; set; }

    /// <summary>
    /// 總使用者數
    /// </summary>
    public int TotalUsers { get; set; }

    /// <summary>
    /// 啟用使用者數
    /// </summary>
    public int ActiveUsers { get; set; }

    /// <summary>
    /// 管理員數量
    /// </summary>
    public int AdminCount { get; set; }
}
