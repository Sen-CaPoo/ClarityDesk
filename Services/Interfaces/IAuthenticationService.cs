using ClarityDesk.Models.DTOs;

namespace ClarityDesk.Services.Interfaces;

/// <summary>
/// 身份驗證服務介面
/// </summary>
public interface IAuthenticationService
{
    /// <summary>
    /// 透過 LINE Login 登入或註冊使用者
    /// </summary>
    /// <param name="lineProfile">LINE 使用者資料</param>
    /// <returns>使用者 DTO</returns>
    Task<UserDto> LoginOrRegisterWithLineAsync(LineUserProfileDto lineProfile);

    /// <summary>
    /// 透過 LINE User ID 取得使用者
    /// </summary>
    /// <param name="lineUserId">LINE User ID</param>
    /// <returns>使用者 DTO (若不存在則回傳 null)</returns>
    Task<UserDto?> GetUserByLineIdAsync(string lineUserId);

    /// <summary>
    /// 檢查使用者是否為管理員
    /// </summary>
    /// <param name="userId">使用者 ID</param>
    /// <returns>是否為管理員</returns>
    Task<bool> IsAdminAsync(int userId);

    /// <summary>
    /// 檢查使用者帳號是否啟用
    /// </summary>
    /// <param name="userId">使用者 ID</param>
    /// <returns>帳號是否啟用</returns>
    Task<bool> IsUserActiveAsync(int userId);

    /// <summary>
    /// 以遊客身份登入（使用通用遊客帳號）
    /// </summary>
    /// <returns>遊客使用者 DTO</returns>
    Task<UserDto> LoginAsGuestAsync();
}
