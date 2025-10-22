using ClarityDesk.Models.DTOs;
using ClarityDesk.Models.Enums;

namespace ClarityDesk.Services.Interfaces;

/// <summary>
/// 使用者管理服務介面
/// </summary>
public interface IUserManagementService
{
    /// <summary>
    /// 取得所有使用者列表
    /// </summary>
    /// <param name="includeInactive">是否包含停用的使用者</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>使用者列表</returns>
    Task<List<UserDto>> GetAllUsersAsync(bool includeInactive = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根據 ID 取得使用者詳細資料
    /// </summary>
    /// <param name="userId">使用者 ID</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>使用者詳細資料</returns>
    Task<UserDto?> GetUserByIdAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新使用者權限角色
    /// </summary>
    /// <param name="userId">使用者 ID</param>
    /// <param name="newRole">新的權限角色</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>是否更新成功</returns>
    Task<bool> UpdateUserRoleAsync(int userId, UserRole newRole, CancellationToken cancellationToken = default);

    /// <summary>
    /// 啟用或停用使用者帳號
    /// </summary>
    /// <param name="userId">使用者 ID</param>
    /// <param name="isActive">是否啟用</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>是否更新成功</returns>
    Task<bool> SetUserActiveStatusAsync(int userId, bool isActive, CancellationToken cancellationToken = default);

    /// <summary>
    /// 取得指定角色的使用者列表
    /// </summary>
    /// <param name="role">使用者角色</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>使用者列表</returns>
    Task<List<UserDto>> GetUsersByRoleAsync(UserRole role, CancellationToken cancellationToken = default);
}
