using ClarityDesk.Models.DTOs;
using ClarityDesk.Models.Entities;

namespace ClarityDesk.Services.Interfaces;

/// <summary>
/// 身份驗證服務介面
/// </summary>
public interface IAuthenticationService
{
    /// <summary>
    /// 透過 LINE OAuth 登入或註冊使用者
    /// </summary>
    /// <param name="lineUserId">LINE User ID</param>
    /// <param name="displayName">顯示名稱</param>
    /// <param name="pictureUrl">頭像 URL</param>
    /// <param name="email">電子信箱 (可選)</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>使用者資訊</returns>
    Task<UserDto> LoginOrRegisterWithLineAsync(
        string lineUserId,
        string displayName,
        string? pictureUrl = null,
        string? email = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 根據 LINE User ID 取得使用者資訊
    /// </summary>
    /// <param name="lineUserId">LINE User ID</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>使用者資訊</returns>
    Task<UserDto?> GetUserByLineIdAsync(string lineUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 驗證使用者是否為管理員
    /// </summary>
    /// <param name="userId">使用者 ID</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>是否為管理員</returns>
    Task<bool> IsAdminAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 驗證使用者是否為活躍狀態
    /// </summary>
    /// <param name="userId">使用者 ID</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>是否為活躍狀態</returns>
    Task<bool> IsUserActiveAsync(int userId, CancellationToken cancellationToken = default);
}
