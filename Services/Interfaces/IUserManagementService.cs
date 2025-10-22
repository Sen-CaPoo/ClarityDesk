using ClarityDesk.Models.DTOs;
using ClarityDesk.Models.Enums;

namespace ClarityDesk.Services.Interfaces;

/// <summary>
/// 使用者管理服務介面
/// </summary>
public interface IUserManagementService
{
    /// <summary>
    /// 取得所有使用者
    /// </summary>
    /// <param name="includeInactive">是否包含停用的使用者</param>
    /// <returns>使用者清單</returns>
    Task<List<UserDto>> GetAllUsersAsync(bool includeInactive = false);

    /// <summary>
    /// 依 ID 取得使用者
    /// </summary>
    /// <param name="userId">使用者 ID</param>
    /// <returns>使用者 DTO,若不存在則回傳 null</returns>
    Task<UserDto?> GetUserByIdAsync(int userId);

    /// <summary>
    /// 更新使用者角色
    /// </summary>
    /// <param name="userId">使用者 ID</param>
    /// <param name="newRole">新角色</param>
    /// <returns>更新是否成功</returns>
    Task<bool> UpdateUserRoleAsync(int userId, UserRole newRole);

    /// <summary>
    /// 設定使用者啟用狀態
    /// </summary>
    /// <param name="userId">使用者 ID</param>
    /// <param name="isActive">是否啟用</param>
    /// <returns>更新是否成功</returns>
    Task<bool> SetUserActiveStatusAsync(int userId, bool isActive);

    /// <summary>
    /// 依角色取得使用者
    /// </summary>
    /// <param name="role">角色</param>
    /// <returns>使用者清單</returns>
    Task<List<UserDto>> GetUsersByRoleAsync(UserRole role);
}
