using ClarityDesk.Data;
using ClarityDesk.Models.DTOs;
using ClarityDesk.Models.Enums;
using ClarityDesk.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace ClarityDesk.Services;

/// <summary>
/// 統計儀表板服務實作
/// </summary>
public class DashboardService : IDashboardService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DashboardService> _logger;
    private readonly IMemoryCache _cache;

    private const string CacheKeyReporters = "Dashboard_Reporters";
    private const string CacheKeyAssignees = "Dashboard_Assignees";
    private const string CacheKeyDepartments = "Dashboard_Departments";
    private const string CacheKeySummary = "Dashboard_Summary";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    /// <summary>
    /// 清除所有儀表板快取
    /// </summary>
    public void ClearAllCaches()
    {
        _logger.LogInformation("清除所有儀表板快取");
        
        // 移除所有可能的快取鍵（包含不同的 topCount 參數）
        for (int i = 1; i <= 50; i++)
        {
            _cache.Remove($"{CacheKeyReporters}_{i}");
            _cache.Remove($"{CacheKeyAssignees}_{i}");
            _cache.Remove($"{CacheKeyDepartments}_{i}");
        }
        _cache.Remove(CacheKeySummary);
    }

    public DashboardService(
        ApplicationDbContext context,
        ILogger<DashboardService> logger,
        IMemoryCache cache)
    {
        _context = context;
        _logger = logger;
        _cache = cache;
    }

    /// <inheritdoc/>
    public async Task<List<ReporterStatisticsDto>> GetReporterStatisticsAsync(
        int topCount = 10,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("開始取得回報人統計資料，取得前 {TopCount} 名", topCount);

        var cacheKey = $"{CacheKeyReporters}_{topCount}";
        if (_cache.TryGetValue<List<ReporterStatisticsDto>>(cacheKey, out var cachedData))
        {
            _logger.LogInformation("從快取中取得回報人統計資料");
            return cachedData!;
        }

        try
        {
            var statistics = await _context.IssueReports
                .AsNoTracking()
                .GroupBy(i => i.ReporterName)
                .Select(g => new ReporterStatisticsDto
                {
                    ReporterName = g.Key,
                    IssueCount = g.Count(),
                    PendingCount = g.Count(i => i.Status == IssueStatus.Pending),
                    ResolvedCount = g.Count(i => i.Status == IssueStatus.Completed)
                })
                .OrderByDescending(r => r.IssueCount)
                .Take(topCount)
                .ToListAsync(cancellationToken);

            _cache.Set(cacheKey, statistics, CacheDuration);
            _logger.LogInformation("成功取得 {Count} 筆回報人統計資料", statistics.Count);

            return statistics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "取得回報人統計資料時發生錯誤");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<List<AssigneeStatisticsDto>> GetAssigneeStatisticsAsync(
        int topCount = 10,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("開始取得處理人統計資料，取得前 {TopCount} 名", topCount);

        var cacheKey = $"{CacheKeyAssignees}_{topCount}";
        if (_cache.TryGetValue<List<AssigneeStatisticsDto>>(cacheKey, out var cachedData))
        {
            _logger.LogInformation("從快取中取得處理人統計資料");
            return cachedData!;
        }

        try
        {
            var statistics = await _context.IssueReports
                .AsNoTracking()
                .Include(i => i.AssignedUser)
                .Where(i => i.AssignedUser != null)
                .GroupBy(i => i.AssignedUser!.DisplayName)
                .Select(g => new AssigneeStatisticsDto
                {
                    AssigneeName = g.Key,
                    AssignedCount = g.Count(),
                    InProgressCount = g.Count(i => i.Status == IssueStatus.Pending),
                    CompletedCount = g.Count(i => i.Status == IssueStatus.Completed)
                })
                .OrderByDescending(a => a.AssignedCount)
                .Take(topCount)
                .ToListAsync(cancellationToken);

            _cache.Set(cacheKey, statistics, CacheDuration);
            _logger.LogInformation("成功取得 {Count} 筆處理人統計資料", statistics.Count);

            return statistics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "取得處理人統計資料時發生錯誤");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<List<DepartmentStatisticsDto>> GetDepartmentStatisticsAsync(
        int topCount = 10,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("開始取得問題所屬單位統計資料，取得前 {TopCount} 名", topCount);

        var cacheKey = $"{CacheKeyDepartments}_{topCount}";
        if (_cache.TryGetValue<List<DepartmentStatisticsDto>>(cacheKey, out var cachedData))
        {
            _logger.LogInformation("從快取中取得問題所屬單位統計資料");
            return cachedData!;
        }

        try
        {
            var statistics = await _context.DepartmentAssignments
                .AsNoTracking()
                .Include(da => da.Department)
                .Include(da => da.IssueReport)
                .Where(da => da.Department != null && da.Department.IsActive)
                .GroupBy(da => da.Department!.Name)
                .Select(g => new DepartmentStatisticsDto
                {
                    DepartmentName = g.Key,
                    IssueCount = g.Count(),
                    NotStartedCount = g.Count(da => da.IssueReport!.Status == IssueStatus.Pending),
                    InProgressCount = 0, // 因為只有兩種狀態，處理中用 0 表示
                    CompletedCount = g.Count(da => da.IssueReport!.Status == IssueStatus.Completed)
                })
                .OrderByDescending(d => d.IssueCount)
                .Take(topCount)
                .ToListAsync(cancellationToken);

            _cache.Set(cacheKey, statistics, CacheDuration);
            _logger.LogInformation("成功取得 {Count} 筆問題所屬單位統計資料", statistics.Count);

            return statistics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "取得問題所屬單位統計資料時發生錯誤");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<DashboardSummaryDto> GetDashboardSummaryAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("開始取得儀表板綜合統計資訊");

        if (_cache.TryGetValue<DashboardSummaryDto>(CacheKeySummary, out var cachedData))
        {
            _logger.LogInformation("從快取中取得儀表板綜合統計資訊");
            return cachedData!;
        }

        try
        {
            var now = DateTime.Now;
            var startOfWeek = now.AddDays(-(int)now.DayOfWeek);
            var startOfMonth = new DateTime(now.Year, now.Month, 1);

            var summary = new DashboardSummaryDto
            {
                TotalIssues = await _context.IssueReports.CountAsync(cancellationToken),
                NotStartedCount = await _context.IssueReports.CountAsync(i => i.Status == IssueStatus.Pending, cancellationToken),
                InProgressCount = 0, // 因為只有兩種狀態，處理中用 0 表示
                CompletedCount = await _context.IssueReports.CountAsync(i => i.Status == IssueStatus.Completed, cancellationToken),
                ThisMonthNewIssues = await _context.IssueReports.CountAsync(i => i.CreatedAt >= startOfMonth, cancellationToken),
                ThisWeekNewIssues = await _context.IssueReports.CountAsync(i => i.CreatedAt >= startOfWeek, cancellationToken)
            };

            _cache.Set(CacheKeySummary, summary, CacheDuration);
            _logger.LogInformation("成功取得儀表板綜合統計資訊");

            return summary;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "取得儀表板綜合統計資訊時發生錯誤");
            throw;
        }
    }
}
