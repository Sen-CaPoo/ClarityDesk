using ClarityDesk.Data;
using ClarityDesk.Models.DTOs;
using ClarityDesk.Models.Entities;
using ClarityDesk.Models.Enums;
using ClarityDesk.Models.Extensions;
using ClarityDesk.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace ClarityDesk.Services;

/// <summary>
/// 回報單管理服務實作
/// </summary>
public class IssueReportService : IIssueReportService
{
    private readonly ApplicationDbContext _context;
    private readonly IMemoryCache _cache;
    private readonly ILogger<IssueReportService> _logger;
    private readonly ILineMessagingService? _lineMessagingService;
    private readonly ILineBindingService? _lineBindingService;

    private const string CacheKeyStatistics = "issue_statistics";
    private const int CacheExpirationMinutes = 5;

    public IssueReportService(
        ApplicationDbContext context,
        IMemoryCache cache,
        ILogger<IssueReportService> logger,
        ILineMessagingService? lineMessagingService = null,
        ILineBindingService? lineBindingService = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _lineMessagingService = lineMessagingService;
        _lineBindingService = lineBindingService;
    }

    /// <inheritdoc />
    public async Task<int> CreateIssueReportAsync(CreateIssueReportDto dto, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("建立回報單: {Title}", dto.Title);

            // 轉換 DTO 為實體
            var issue = dto.ToEntity();

            // 新增至資料庫
            await _context.IssueReports.AddAsync(issue, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            // 建立單位指派關聯
            if (dto.DepartmentIds != null && dto.DepartmentIds.Any())
            {
                var assignments = dto.DepartmentIds.Select(deptId => new DepartmentAssignment
                {
                    IssueReportId = issue.Id,
                    DepartmentId = deptId,
                    AssignedAt = DateTime.UtcNow
                }).ToList();

                await _context.DepartmentAssignments.AddRangeAsync(assignments, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
            }

            // 清除統計快取
            _cache.Remove(CacheKeyStatistics);

            _logger.LogInformation("成功建立回報單 ID: {IssueId}", issue.Id);

            // 重新查詢完整的回報單資料 (包含單位與處理人員)
            var issueDto = await GetIssueReportByIdAsync(issue.Id, cancellationToken);

            // 發送 LINE 推送通知 (等待完成以確保發送成功,但不影響回報單建立結果)
            if (issueDto != null)
            {
                await SendLineNotificationAsync(issue.Id, issueDto, cancellationToken);
            }

            return issue.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "建立回報單時發生錯誤");
            throw;
        }
    }

    /// <summary>
    /// 發送 LINE 推送通知給指派的處理人員
    /// </summary>
    private async Task SendLineNotificationAsync(int issueId, IssueReportDto issueDto, CancellationToken cancellationToken)
    {
        try
        {
            // 如果服務未註冊,跳過通知
            if (_lineMessagingService == null || _lineBindingService == null)
            {
                _logger.LogDebug("LINE 服務未啟用,跳過推送通知");
                return;
            }

            // 發送通知給指派的處理人員
            if (issueDto.AssignedUserId > 0)
            {
                // 檢查處理人員是否已綁定 LINE
                var binding = await _lineBindingService.GetBindingByUserIdAsync(issueDto.AssignedUserId, cancellationToken);
                
                if (binding != null && binding.BindingStatus == BindingStatus.Active)
                {
                    var success = await _lineMessagingService.SendIssueNotificationAsync(
                        binding.LineUserId,
                        issueDto,
                        cancellationToken);

                    if (success)
                    {
                        _logger.LogInformation("成功發送 LINE 通知: IssueId={IssueId}, UserId={UserId}, LineUserId={LineUserId}",
                            issueId, issueDto.AssignedUserId, binding.LineUserId);
                    }
                    else
                    {
                        _logger.LogWarning("LINE 通知發送失敗: IssueId={IssueId}, UserId={UserId}",
                            issueId, issueDto.AssignedUserId);
                    }
                }
                else
                {
                    _logger.LogDebug("處理人員未綁定 LINE 或狀態非 Active: UserId={UserId}, BindingStatus={BindingStatus}",
                        issueDto.AssignedUserId, binding?.BindingStatus.ToString() ?? "null");
                }
            }
            else
            {
                _logger.LogDebug("回報單未指派處理人員,跳過 LINE 通知: IssueId={IssueId}", issueId);
            }
        }
        catch (Exception ex)
        {
            // 通知失敗不影響回報單建立,僅記錄錯誤
            _logger.LogError(ex, "發送 LINE 通知時發生錯誤: IssueId={IssueId}", issueId);
        }
    }

    /// <inheritdoc />
    public async Task<bool> UpdateIssueReportAsync(int id, UpdateIssueReportDto dto, int currentUserId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("更新回報單 ID: {IssueId}, 修改人: {UserId}", id, currentUserId);

            var issue = await _context.IssueReports
                .Include(i => i.DepartmentAssignments)
                .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

            if (issue == null)
            {
                _logger.LogWarning("找不到回報單 ID: {IssueId}", id);
                return false;
            }

            // 更新實體，並檢查是否有實際變更
            bool hasEntityChanges = issue.UpdateFromDto(dto);

            // 檢查單位指派是否有變更
            bool hasDepartmentChanges = false;
            if (dto.DepartmentIds != null)
            {
                var currentDepartmentIds = issue.DepartmentAssignments.Select(da => da.DepartmentId).OrderBy(id => id).ToList();
                var newDepartmentIds = dto.DepartmentIds.OrderBy(id => id).ToList();

                // 比較單位指派是否有變更
                if (!currentDepartmentIds.SequenceEqual(newDepartmentIds))
                {
                    hasDepartmentChanges = true;

                    // 移除舊的指派
                    _context.DepartmentAssignments.RemoveRange(issue.DepartmentAssignments);

                    // 新增新的指派
                    var newAssignments = dto.DepartmentIds.Select(deptId => new DepartmentAssignment
                    {
                        IssueReportId = issue.Id,
                        DepartmentId = deptId,
                        AssignedAt = DateTime.UtcNow
                    }).ToList();

                    await _context.DepartmentAssignments.AddRangeAsync(newAssignments, cancellationToken);

                    // 如果單位有變更但實體欄位沒變更，仍需更新 UpdatedAt
                    if (!hasEntityChanges)
                    {
                        issue.UpdatedAt = DateTime.UtcNow;
                    }
                }
            }

            // 只有在有變更時才更新最後修改人
            if (hasEntityChanges || hasDepartmentChanges)
            {
                issue.LastModifiedByUserId = currentUserId;
            }

            await _context.SaveChangesAsync(cancellationToken);

            // 清除統計快取
            if (hasEntityChanges || hasDepartmentChanges)
            {
                _cache.Remove(CacheKeyStatistics);
            }

            _logger.LogInformation("成功更新回報單 ID: {IssueId}, 有變更: {HasChanges}", id, hasEntityChanges || hasDepartmentChanges);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新回報單時發生錯誤, ID: {IssueId}", id);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> DeleteIssueReportAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("刪除回報單 ID: {IssueId}", id);

            var issue = await _context.IssueReports
                .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

            if (issue == null)
            {
                _logger.LogWarning("找不到回報單 ID: {IssueId}", id);
                return false;
            }

            _context.IssueReports.Remove(issue);
            await _context.SaveChangesAsync(cancellationToken);

            // 清除統計快取
            _cache.Remove(CacheKeyStatistics);

            _logger.LogInformation("成功刪除回報單 ID: {IssueId}", id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "刪除回報單時發生錯誤, ID: {IssueId}", id);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IssueReportDto?> GetIssueReportByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var issue = await _context.IssueReports
                .Include(i => i.AssignedUser)
                .Include(i => i.LastModifiedBy)
                .Include(i => i.DepartmentAssignments)
                    .ThenInclude(da => da.Department)
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

            return issue?.ToDto();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "取得回報單時發生錯誤, ID: {IssueId}", id);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<PagedResult<IssueReportDto>> GetIssueReportsAsync(
        IssueFilterDto filter,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("GetIssueReportsAsync - Filter: Status={Status}, Priority={Priority}, AssignedUserIds={AssignedUserIds}, StartDate={StartDate}, EndDate={EndDate}, SearchKeyword={SearchKeyword}, DepartmentIds={DepartmentIds}",
                filter.Status, filter.Priority, filter.AssignedUserIds != null ? string.Join(",", filter.AssignedUserIds) : "null", filter.StartDate, filter.EndDate, filter.SearchKeyword, filter.DepartmentIds != null ? string.Join(",", filter.DepartmentIds) : "null");

            var query = _context.IssueReports
                .Include(i => i.AssignedUser)
                .Include(i => i.DepartmentAssignments)
                    .ThenInclude(da => da.Department)
                .AsNoTracking()
                .AsQueryable();

            // 套用篩選條件
            if (filter.Status.HasValue)
            {
                _logger.LogInformation("Filtering by Status: {Status}", filter.Status.Value);
                query = query.Where(i => i.Status == filter.Status.Value);
            }

            if (filter.Priority.HasValue)
            {
                _logger.LogInformation("Filtering by Priority: {Priority} (int value: {PriorityInt})", filter.Priority.Value, (int)filter.Priority.Value);
                query = query.Where(i => i.Priority == filter.Priority.Value);
            }

            if (filter.AssignedUserIds != null && filter.AssignedUserIds.Any())
            {
                _logger.LogInformation("Filtering by AssignedUserIds: {AssignedUserIds}", string.Join(",", filter.AssignedUserIds));
                query = query.Where(i => filter.AssignedUserIds.Contains(i.AssignedUserId));
            }

            if (filter.StartDate.HasValue)
            {
                var startDate = filter.StartDate.Value.Date; // 只取日期部分
                _logger.LogInformation("Filtering by StartDate: {StartDate} (Date only: {DateOnly})", filter.StartDate.Value, startDate);
                query = query.Where(i => i.RecordDate.Date >= startDate);
            }

            if (filter.EndDate.HasValue)
            {
                var endDate = filter.EndDate.Value.Date; // 只取日期部分
                _logger.LogInformation("Filtering by EndDate: {EndDate} (Date only: {DateOnly})", filter.EndDate.Value, endDate);
                query = query.Where(i => i.RecordDate.Date <= endDate);
            }

            if (!string.IsNullOrWhiteSpace(filter.SearchKeyword))
            {
                var keyword = filter.SearchKeyword.ToLower();
                query = query.Where(i =>
                    i.Title.ToLower().Contains(keyword) ||
                    i.Content.ToLower().Contains(keyword) ||
                    i.CustomerName.ToLower().Contains(keyword));
            }

            if (filter.DepartmentIds != null && filter.DepartmentIds.Any())
            {
                query = query.Where(i => i.DepartmentAssignments
                    .Any(da => filter.DepartmentIds.Contains(da.DepartmentId)));
            }

            // 計算總筆數
            var totalCount = await query.CountAsync(cancellationToken);
            
            _logger.LogInformation("After filtering, total count: {TotalCount}", totalCount);

            // 套用排序
            query = filter.SortBy?.ToLower() switch
            {
                "title" => filter.SortDescending
                    ? query.OrderByDescending(i => i.Title)
                    : query.OrderBy(i => i.Title),
                "status" => filter.SortDescending
                    ? query.OrderByDescending(i => i.Status)
                    : query.OrderBy(i => i.Status),
                "priority" => filter.SortDescending
                    ? query.OrderByDescending(i => i.Priority)
                    : query.OrderBy(i => i.Priority),
                "recorddate" => filter.SortDescending
                    ? query.OrderByDescending(i => i.RecordDate)
                    : query.OrderBy(i => i.RecordDate),
                _ => filter.SortDescending
                    ? query.OrderByDescending(i => i.CreatedAt)
                    : query.OrderBy(i => i.CreatedAt)
            };

            // 套用分頁
            var issues = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var issueDtos = issues.Select(i => i.ToDto()).ToList();

            return PagedResult<IssueReportDto>.Create(issueDtos, totalCount, page, pageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "取得回報單列表時發生錯誤");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IssueStatisticsDto> GetIssueStatisticsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // 嘗試從快取取得
            if (_cache.TryGetValue(CacheKeyStatistics, out IssueStatisticsDto? cachedStats) && cachedStats != null)
            {
                return cachedStats;
            }

            // 計算統計資訊
            var totalIssues = await _context.IssueReports.CountAsync(cancellationToken);
            var pendingIssues = await _context.IssueReports.CountAsync(i => i.Status == IssueStatus.Pending, cancellationToken);
            var completedIssues = await _context.IssueReports.CountAsync(i => i.Status == IssueStatus.Completed, cancellationToken);
            var highPriorityIssues = await _context.IssueReports.CountAsync(i => i.Priority == PriorityLevel.High, cancellationToken);

            var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
            var thisMonthIssues = await _context.IssueReports
                .CountAsync(i => i.CreatedAt >= startOfMonth, cancellationToken);

            var statistics = new IssueStatisticsDto
            {
                TotalIssues = totalIssues,
                PendingIssues = pendingIssues,
                CompletedIssues = completedIssues,
                HighPriorityIssues = highPriorityIssues,
                ThisMonthIssues = thisMonthIssues,
                StatisticsTime = DateTime.UtcNow
            };

            // 存入快取 (5 分鐘)
            _cache.Set(CacheKeyStatistics, statistics, TimeSpan.FromMinutes(CacheExpirationMinutes));

            return statistics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "取得統計資訊時發生錯誤");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> UpdateIssueStatusAsync(int id, IssueStatus newStatus, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("更新回報單狀態, ID: {IssueId}, 新狀態: {Status}", id, newStatus);

            var issue = await _context.IssueReports
                .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

            if (issue == null)
            {
                _logger.LogWarning("找不到回報單 ID: {IssueId}", id);
                return false;
            }

            issue.Status = newStatus;
            issue.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            // 清除統計快取
            _cache.Remove(CacheKeyStatistics);

            _logger.LogInformation("成功更新回報單狀態, ID: {IssueId}", id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新回報單狀態時發生錯誤, ID: {IssueId}", id);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> AssignIssueToUserAsync(int id, int assignedUserId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("指派回報單, ID: {IssueId}, 使用者 ID: {UserId}", id, assignedUserId);

            var issue = await _context.IssueReports
                .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

            if (issue == null)
            {
                _logger.LogWarning("找不到回報單 ID: {IssueId}", id);
                return false;
            }

            issue.AssignedUserId = assignedUserId;
            issue.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("成功指派回報單, ID: {IssueId}", id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "指派回報單時發生錯誤, ID: {IssueId}", id);
            throw;
        }
    }
}
