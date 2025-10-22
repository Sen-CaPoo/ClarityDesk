using ClarityDesk.Data;
using ClarityDesk.Models.DTOs;
using ClarityDesk.Models.Entities;
using ClarityDesk.Models.Enums;
using ClarityDesk.Models.Extensions;
using ClarityDesk.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ClarityDesk.Services;

/// <summary>
/// 身份驗證服務實作
/// </summary>
public class AuthenticationService : IAuthenticationService
{
    private readonly ApplicationDbContext _context;

    public AuthenticationService(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// 透過 LINE Login 登入或註冊使用者
    /// </summary>
    public async Task<UserDto> LoginOrRegisterWithLineAsync(LineUserProfileDto lineProfile)
    {
        // 檢查使用者是否已存在
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.LineUserId == lineProfile.UserId);

        if (existingUser != null)
        {
            // 更新使用者資訊 (頭像、顯示名稱可能變更)
            existingUser.DisplayName = lineProfile.DisplayName;
            existingUser.PictureUrl = lineProfile.PictureUrl;
            existingUser.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return existingUser.ToDto();
        }

        // 建立新使用者 (預設角色為 User)
        var newUser = new User
        {
            LineUserId = lineProfile.UserId,
            DisplayName = lineProfile.DisplayName,
            PictureUrl = lineProfile.PictureUrl,
            Role = UserRole.User,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Users.Add(newUser);
        await _context.SaveChangesAsync();

        return newUser.ToDto();
    }

    /// <summary>
    /// 透過 LINE User ID 取得使用者
    /// </summary>
    public async Task<UserDto?> GetUserByLineIdAsync(string lineUserId)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.LineUserId == lineUserId);

        return user?.ToDto();
    }

    /// <summary>
    /// 檢查使用者是否為管理員
    /// </summary>
    public async Task<bool> IsAdminAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        return user?.Role == UserRole.Admin;
    }

    /// <summary>
    /// 檢查使用者帳號是否啟用
    /// </summary>
    public async Task<bool> IsUserActiveAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        return user?.IsActive ?? false;
    }
}
