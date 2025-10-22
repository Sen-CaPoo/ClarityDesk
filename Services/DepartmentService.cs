using ClarityDesk.Data;
using ClarityDesk.Models.DTOs;
using ClarityDesk.Models.Entities;
using ClarityDesk.Models.Extensions;
using ClarityDesk.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace ClarityDesk.Services;

/// <summary>
/// 單位管理服務
/// </summary>
public class DepartmentService : IDepartmentService
{
    private readonly ApplicationDbContext _context;
    private readonly IMemoryCache _cache;
    private readonly ILogger<DepartmentService> _logger;
    private const string CacheKeyPrefix = "Department_";
    private const string AllDepartmentsCacheKey = "AllDepartments";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(1);

    public DepartmentService(
        ApplicationDbContext context,
        IMemoryCache cache,
        ILogger<DepartmentService>? logger = null)
    {
        _context = context;
        _cache = cache;
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<DepartmentService>.Instance;
    }

    /// <summary>
    /// 建立單位
    /// </summary>
    public async Task<int> CreateDepartmentAsync(CreateDepartmentDto dto)
    {
        try
        {
            var department = dto.ToEntity();

            _context.Departments.Add(department);
            await _context.SaveChangesAsync();

            // 清除快取
            ClearCache();

            _logger.LogInformation("已建立新單位: {DepartmentName} (ID: {DepartmentId})", department.Name, department.Id);
            return department.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "建立單位時發生錯誤: {DepartmentName}", dto.Name);
            throw;
        }
    }

    /// <summary>
    /// 更新單位
    /// </summary>
    public async Task<bool> UpdateDepartmentAsync(int id, UpdateDepartmentDto dto)
    {
        try
        {
            var department = await _context.Departments.FindAsync(id);

            if (department == null)
            {
                _logger.LogWarning("嘗試更新不存在的單位 ID: {DepartmentId}", id);
                return false;
            }

            department.UpdateFromDto(dto);
            await _context.SaveChangesAsync();

            // 清除快取
            ClearCache();
            _cache.Remove($"{CacheKeyPrefix}{id}");

            _logger.LogInformation("已更新單位: {DepartmentName} (ID: {DepartmentId})", department.Name, id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新單位時發生錯誤: ID {DepartmentId}", id);
            throw;
        }
    }

    /// <summary>
    /// 刪除單位 (軟刪除)
    /// </summary>
    public async Task<bool> DeleteDepartmentAsync(int id)
    {
        try
        {
            var department = await _context.Departments.FindAsync(id);

            if (department == null)
            {
                _logger.LogWarning("嘗試刪除不存在的單位 ID: {DepartmentId}", id);
                return false;
            }

            // 軟刪除: 設定 IsActive = false
            department.IsActive = false;
            department.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // 清除快取
            ClearCache();
            _cache.Remove($"{CacheKeyPrefix}{id}");

            _logger.LogInformation("已軟刪除單位: {DepartmentName} (ID: {DepartmentId})", department.Name, id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "刪除單位時發生錯誤: ID {DepartmentId}", id);
            throw;
        }
    }

    /// <summary>
    /// 取得所有單位
    /// </summary>
    public async Task<IEnumerable<DepartmentDto>> GetAllDepartmentsAsync(bool activeOnly = true)
    {
        try
        {
            var cacheKey = $"{AllDepartmentsCacheKey}_{activeOnly}";

            if (_cache.TryGetValue<IEnumerable<DepartmentDto>>(cacheKey, out var cachedDepartments))
            {
                _logger.LogDebug("從快取取得單位清單 (activeOnly: {ActiveOnly})", activeOnly);
                return cachedDepartments!;
            }

            var query = _context.Departments.AsNoTracking();

            if (activeOnly)
            {
                query = query.Where(d => d.IsActive);
            }

            var departments = await query
                .OrderBy(d => d.Name)
                .ToListAsync();

            var departmentDtos = new List<DepartmentDto>();

            foreach (var department in departments)
            {
                var dto = department.ToDto();

                // 載入處理人員資訊
                var assignedUserIds = await _context.DepartmentUsers
                    .AsNoTracking()
                    .Where(du => du.DepartmentId == department.Id)
                    .Select(du => du.UserId)
                    .ToListAsync();

                if (assignedUserIds.Any())
                {
                    var users = await _context.Users
                        .AsNoTracking()
                        .Where(u => assignedUserIds.Contains(u.Id))
                        .ToListAsync();

                    dto.AssignedUsers = users.Select(u => u.ToDto()).ToList();
                }
                else
                {
                    dto.AssignedUsers = new List<UserDto>();
                }

                departmentDtos.Add(dto);
            }

            // 快取 1 小時
            _cache.Set(cacheKey, departmentDtos, CacheDuration);

            _logger.LogInformation("已取得 {Count} 個單位 (activeOnly: {ActiveOnly})", departmentDtos.Count, activeOnly);
            return departmentDtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "取得單位清單時發生錯誤");
            throw;
        }
    }

    /// <summary>
    /// 依 ID 取得單位
    /// </summary>
    public async Task<DepartmentDto?> GetDepartmentByIdAsync(int id)
    {
        try
        {
            var cacheKey = $"{CacheKeyPrefix}{id}";

            if (_cache.TryGetValue<DepartmentDto>(cacheKey, out var cachedDepartment))
            {
                _logger.LogDebug("從快取取得單位 ID: {DepartmentId}", id);
                return cachedDepartment;
            }

            var department = await _context.Departments
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == id);

            if (department == null)
            {
                _logger.LogWarning("找不到單位 ID: {DepartmentId}", id);
                return null;
            }

            var dto = department.ToDto();

            // 取得指派的處理人員
            var assignedUserIds = await _context.DepartmentUsers
                .AsNoTracking()
                .Where(du => du.DepartmentId == id)
                .Select(du => du.UserId)
                .ToListAsync();

            if (assignedUserIds.Any())
            {
                var users = await _context.Users
                    .AsNoTracking()
                    .Where(u => assignedUserIds.Contains(u.Id))
                    .ToListAsync();

                dto.AssignedUsers = users.Select(u => u.ToDto()).ToList();
            }
            else
            {
                // 確保 AssignedUsers 不為 null
                dto.AssignedUsers = new List<UserDto>();
            }

            // 快取 1 小時
            _cache.Set(cacheKey, dto, CacheDuration);

            return dto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "取得單位時發生錯誤: ID {DepartmentId}", id);
            throw;
        }
    }

    /// <summary>
    /// 指派使用者到單位
    /// </summary>
    public async Task<bool> AssignUsersToDepartmentAsync(int departmentId, List<int> userIds)
    {
        try
        {
            var department = await _context.Departments.FindAsync(departmentId);

            if (department == null)
            {
                _logger.LogWarning("嘗試指派使用者到不存在的單位 ID: {DepartmentId}", departmentId);
                return false;
            }

            // 移除舊的指派關係
            var existingAssignments = await _context.DepartmentUsers
                .Where(du => du.DepartmentId == departmentId)
                .ToListAsync();

            _context.DepartmentUsers.RemoveRange(existingAssignments);

            // 建立新的指派關係
            var now = DateTime.UtcNow;
            var newAssignments = userIds.Select(userId => new DepartmentUser
            {
                DepartmentId = departmentId,
                UserId = userId,
                AssignedAt = now
            }).ToList();

            _context.DepartmentUsers.AddRange(newAssignments);
            await _context.SaveChangesAsync();

            // 清除快取
            ClearCache(); // 清除列表快取
            _cache.Remove($"{CacheKeyPrefix}{departmentId}"); // 清除單一單位快取

            _logger.LogInformation("已為單位 {DepartmentId} 指派 {UserCount} 位處理人員", departmentId, userIds.Count);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "指派使用者到單位時發生錯誤: DepartmentId {DepartmentId}", departmentId);
            throw;
        }
    }

    /// <summary>
    /// 取得單位的處理人員清單
    /// </summary>
    public async Task<IEnumerable<UserDto>> GetDepartmentUsersAsync(int departmentId)
    {
        try
        {
            var userIds = await _context.DepartmentUsers
                .AsNoTracking()
                .Where(du => du.DepartmentId == departmentId)
                .Select(du => du.UserId)
                .ToListAsync();

            if (!userIds.Any())
            {
                return new List<UserDto>();
            }

            var users = await _context.Users
                .AsNoTracking()
                .Where(u => userIds.Contains(u.Id) && u.IsActive)
                .OrderBy(u => u.DisplayName)
                .ToListAsync();

            return users.Select(u => u.ToDto()).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "取得單位處理人員時發生錯誤: DepartmentId {DepartmentId}", departmentId);
            throw;
        }
    }

    /// <summary>
    /// 清除所有單位相關快取
    /// </summary>
    private void ClearCache()
    {
        _cache.Remove($"{AllDepartmentsCacheKey}_True");
        _cache.Remove($"{AllDepartmentsCacheKey}_False");
    }
}
