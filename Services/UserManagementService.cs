using ClarityDesk.Data;
using ClarityDesk.Models.DTOs;
using ClarityDesk.Models.Enums;
using ClarityDesk.Models.Extensions;
using ClarityDesk.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace ClarityDesk.Services;

/// <summary>
/// 使用者管理服務實作
/// </summary>
public class UserManagementService : IUserManagementService
{
    private readonly ApplicationDbContext _context;
    private readonly IMemoryCache? _cache;
    private const string UsersCacheKey = "all_users";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

    public UserManagementService(ApplicationDbContext context, IMemoryCache? cache)
    {
        _context = context;
        _cache = cache;
    }

    /// <summary>
    /// 取得所有使用者
    /// </summary>
    public async Task<List<UserDto>> GetAllUsersAsync(bool includeInactive = false)
    {
        // 移除快取機制,直接從資料庫讀取以確保資料即時性
        var query = _context.Users.AsNoTracking().AsQueryable();

        if (!includeInactive)
        {
            query = query.Where(u => u.IsActive);
        }

        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Select(u => u.ToDto())
            .ToListAsync();

        return users;
    }

    /// <summary>
    /// 依 ID 取得使用者
    /// </summary>
    public async Task<UserDto?> GetUserByIdAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        return user?.ToDto();
    }

    /// <summary>
    /// 更新使用者角色
    /// </summary>
    public async Task<bool> UpdateUserRoleAsync(int userId, UserRole newRole)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return false;
        }

        user.Role = newRole;
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // 清除快取
        ClearUsersCache();

        return true;
    }

    /// <summary>
    /// 設定使用者啟用狀態
    /// </summary>
    public async Task<bool> SetUserActiveStatusAsync(int userId, bool isActive)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return false;
        }

        user.IsActive = isActive;
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // 清除快取
        ClearUsersCache();

        return true;
    }

    /// <summary>
    /// 依角色取得使用者
    /// </summary>
    public async Task<List<UserDto>> GetUsersByRoleAsync(UserRole role)
    {
        return await _context.Users
            .Where(u => u.Role == role && u.IsActive)
            .OrderBy(u => u.DisplayName)
            .Select(u => u.ToDto())
            .ToListAsync();
    }

    /// <summary>
    /// 更新使用者資料
    /// </summary>
    public async Task<bool> UpdateUserAsync(int userId, UpdateUserDto updateDto)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return false;
        }

        user.DisplayName = updateDto.DisplayName;
        user.Email = updateDto.Email;
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return true;
    }

    /// <summary>
    /// 清除使用者快取
    /// </summary>
    private void ClearUsersCache()
    {
        _cache?.Remove($"{UsersCacheKey}_true");
        _cache?.Remove($"{UsersCacheKey}_false");
    }
}
