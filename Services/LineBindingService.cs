using ClarityDesk.Data;
using ClarityDesk.Models.DTOs;
using ClarityDesk.Models.Entities;
using ClarityDesk.Models.Enums;
using ClarityDesk.Models.Extensions;
using ClarityDesk.Services.Exceptions;
using ClarityDesk.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace ClarityDesk.Services;

/// <summary>
/// LINE 帳號綁定服務實作
/// 管理 ClarityDesk 使用者與 LINE 帳號的綁定關係
/// </summary>
public class LineBindingService : ILineBindingService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<LineBindingService> _logger;
    private readonly IMemoryCache _cache;
    private const string CacheKeyPrefix = "LineBinding_User_";
    private const string LineUserCacheKeyPrefix = "LineBinding_LineUser_";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public LineBindingService(
        ApplicationDbContext context,
        ILogger<LineBindingService> logger,
        IMemoryCache cache)
    {
        _context = context;
        _logger = logger;
        _cache = cache;
    }

    /// <summary>
    /// 建立或更新使用者的 LINE 綁定關係
    /// </summary>
    public async Task<int> CreateOrUpdateBindingAsync(
        int userId,
        string lineUserId,
        string displayName,
        string? pictureUrl = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("開始處理 LINE 綁定: UserId={UserId}, LineUserId={LineUserId}", userId, lineUserId);

            // 檢查使用者是否為訪客帳號
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

            if (user == null)
            {
                throw new InvalidOperationException($"找不到使用者 ID: {userId}");
            }

            // 訪客帳號透過 LineUserId = "guest" 識別,不允許綁定
            if (user.LineUserId == "guest")
            {
                _logger.LogWarning("訪客帳號嘗試綁定 LINE: UserId={UserId}", userId);
                throw new InvalidOperationException("訪客帳號無法綁定 LINE 官方帳號");
            }

            // 檢查此 LINE User ID 是否已被其他帳號綁定
            var existingBinding = await _context.LineBindings
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.LineUserId == lineUserId, cancellationToken);

            if (existingBinding != null && existingBinding.UserId != userId)
            {
                _logger.LogWarning("LINE User ID 已被其他帳號綁定: LineUserId={LineUserId}, ExistingUserId={ExistingUserId}, RequestUserId={RequestUserId}",
                    lineUserId, existingBinding.UserId, userId);
                throw new LineBindingException($"此 LINE 帳號已被其他帳號綁定");
            }

            // 查找現有綁定記錄
            var binding = await _context.LineBindings
                .FirstOrDefaultAsync(b => b.UserId == userId, cancellationToken);

            if (binding == null)
            {
                // 建立新綁定
                var now = DateTime.UtcNow;
                binding = new LineBinding
                {
                    UserId = userId,
                    LineUserId = lineUserId,
                    DisplayName = displayName,
                    PictureUrl = pictureUrl,
                    BindingStatus = BindingStatus.Active,
                    BoundAt = now,
                    LastInteractedAt = now,
                    CreatedAt = now,
                    UpdatedAt = now
                };
                _context.LineBindings.Add(binding);
                _logger.LogInformation("建立新 LINE 綁定: UserId={UserId}, LineUserId={LineUserId}", userId, lineUserId);
            }
            else
            {
                // 更新現有綁定
                binding.LineUserId = lineUserId;
                binding.DisplayName = displayName;
                binding.PictureUrl = pictureUrl;
                binding.BindingStatus = BindingStatus.Active;
                binding.LastInteractedAt = DateTime.UtcNow;
                binding.UpdatedAt = DateTime.UtcNow;
                _logger.LogInformation("更新現有 LINE 綁定: UserId={UserId}, LineUserId={LineUserId}", userId, lineUserId);
            }

            await _context.SaveChangesAsync(cancellationToken);

            // 清除快取
            ClearBindingCache(userId, lineUserId);

            _logger.LogInformation("LINE 綁定處理完成: UserId={UserId}, BindingId={BindingId}", userId, binding.Id);
            return binding.Id;
        }
        catch (Exception ex) when (ex is not LineBindingException && ex is not InvalidOperationException)
        {
            _logger.LogError(ex, "建立或更新 LINE 綁定時發生錯誤: UserId={UserId}, LineUserId={LineUserId}", userId, lineUserId);
            throw;
        }
    }

    /// <summary>
    /// 取得使用者的 LINE 綁定資訊
    /// </summary>
    public async Task<LineBindingDto?> GetBindingByUserIdAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = $"{CacheKeyPrefix}{userId}";

            if (_cache.TryGetValue(cacheKey, out LineBindingDto? cachedDto))
            {
                return cachedDto;
            }

            var binding = await _context.LineBindings
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.UserId == userId, cancellationToken);

            if (binding == null)
            {
                return null;
            }

            var dto = binding.ToDto();
            _cache.Set(cacheKey, dto, CacheDuration);

            return dto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "取得使用者 LINE 綁定資訊時發生錯誤: UserId={UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// 依 LINE User ID 查詢綁定資訊
    /// </summary>
    public async Task<LineBindingDto?> GetBindingByLineUserIdAsync(
        string lineUserId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = $"{LineUserCacheKeyPrefix}{lineUserId}";

            if (_cache.TryGetValue(cacheKey, out LineBindingDto? cachedDto))
            {
                return cachedDto;
            }

            var binding = await _context.LineBindings
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.LineUserId == lineUserId, cancellationToken);

            if (binding == null)
            {
                return null;
            }

            var dto = binding.ToDto();
            _cache.Set(cacheKey, dto, CacheDuration);

            return dto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "依 LINE User ID 查詢綁定資訊時發生錯誤: LineUserId={LineUserId}", lineUserId);
            throw;
        }
    }

    /// <summary>
    /// 檢查使用者是否已綁定 LINE 帳號
    /// </summary>
    public async Task<bool> IsUserBoundAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.LineBindings
                .AsNoTracking()
                .AnyAsync(b => b.UserId == userId && b.BindingStatus == BindingStatus.Active, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "檢查使用者綁定狀態時發生錯誤: UserId={UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// 解除使用者的 LINE 綁定
    /// </summary>
    public async Task<bool> UnbindAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("開始解除 LINE 綁定: UserId={UserId}", userId);

            var binding = await _context.LineBindings
                .FirstOrDefaultAsync(b => b.UserId == userId, cancellationToken);

            if (binding == null)
            {
                _logger.LogWarning("嘗試解除不存在的綁定: UserId={UserId}", userId);
                return false;
            }

            binding.BindingStatus = BindingStatus.Unbound;
            binding.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            // 清除快取
            ClearBindingCache(userId, binding.LineUserId);

            _logger.LogInformation("LINE 綁定已解除: UserId={UserId}, BindingId={BindingId}", userId, binding.Id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "解除 LINE 綁定時發生錯誤: UserId={UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// 標記綁定狀態為「已封鎖」
    /// </summary>
    public async Task MarkAsBlockedAsync(
        string lineUserId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("標記 LINE 綁定為封鎖狀態: LineUserId={LineUserId}", lineUserId);

            var binding = await _context.LineBindings
                .FirstOrDefaultAsync(b => b.LineUserId == lineUserId, cancellationToken);

            if (binding == null)
            {
                _logger.LogWarning("嘗試標記不存在的綁定為封鎖: LineUserId={LineUserId}", lineUserId);
                return;
            }

            binding.BindingStatus = BindingStatus.Blocked;
            binding.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            // 清除快取
            ClearBindingCache(binding.UserId, lineUserId);

            _logger.LogInformation("LINE 綁定已標記為封鎖: LineUserId={LineUserId}, BindingId={BindingId}", lineUserId, binding.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "標記 LINE 綁定為封鎖時發生錯誤: LineUserId={LineUserId}", lineUserId);
            throw;
        }
    }

    /// <summary>
    /// 標記綁定狀態為「正常」
    /// </summary>
    public async Task MarkAsActiveAsync(
        string lineUserId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("標記 LINE 綁定為正常狀態: LineUserId={LineUserId}", lineUserId);

            var binding = await _context.LineBindings
                .FirstOrDefaultAsync(b => b.LineUserId == lineUserId, cancellationToken);

            if (binding == null)
            {
                _logger.LogWarning("嘗試標記不存在的綁定為正常: LineUserId={LineUserId}", lineUserId);
                return;
            }

            binding.BindingStatus = BindingStatus.Active;
            binding.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            // 清除快取
            ClearBindingCache(binding.UserId, lineUserId);

            _logger.LogInformation("LINE 綁定已標記為正常: LineUserId={LineUserId}, BindingId={BindingId}", lineUserId, binding.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "標記 LINE 綁定為正常時發生錯誤: LineUserId={LineUserId}", lineUserId);
            throw;
        }
    }

    /// <summary>
    /// 更新最後互動時間
    /// </summary>
    public async Task UpdateLastInteractionAsync(
        string lineUserId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var binding = await _context.LineBindings
                .FirstOrDefaultAsync(b => b.LineUserId == lineUserId, cancellationToken);

            if (binding == null)
            {
                _logger.LogWarning("嘗試更新不存在綁定的互動時間: LineUserId={LineUserId}", lineUserId);
                return;
            }

            binding.LastInteractedAt = DateTime.UtcNow;
            binding.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            // 清除快取
            ClearBindingCache(binding.UserId, lineUserId);

            _logger.LogDebug("已更新 LINE 綁定的最後互動時間: LineUserId={LineUserId}", lineUserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新 LINE 綁定互動時間時發生錯誤: LineUserId={LineUserId}", lineUserId);
            throw;
        }
    }

    /// <summary>
    /// 取得所有已綁定的使用者列表
    /// </summary>
    public async Task<PagedResult<LineBindingDto>> GetAllBindingsAsync(
        BindingStatus? status = null,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("查詢 LINE 綁定列表: Status={Status}, PageNumber={PageNumber}, PageSize={PageSize}",
                status, pageNumber, pageSize);

            var query = _context.LineBindings.AsNoTracking();

            if (status.HasValue)
            {
                query = query.Where(b => b.BindingStatus == status.Value);
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var bindings = await query
                .OrderByDescending(b => b.BoundAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var dtos = bindings.Select(b => b.ToDto()).ToList();

            return PagedResult<LineBindingDto>.Create(dtos, totalCount, pageNumber, pageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查詢 LINE 綁定列表時發生錯誤");
            throw;
        }
    }

    /// <summary>
    /// 清除綁定快取
    /// </summary>
    private void ClearBindingCache(int userId, string lineUserId)
    {
        _cache.Remove($"{CacheKeyPrefix}{userId}");
        _cache.Remove($"{LineUserCacheKeyPrefix}{lineUserId}");
    }
}
