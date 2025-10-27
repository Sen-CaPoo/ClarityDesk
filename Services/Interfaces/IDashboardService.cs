using ClarityDesk.Models.DTOs;

namespace ClarityDesk.Services.Interfaces;

/// <summary>
/// 統計儀表板服務介面
/// </summary>
public interface IDashboardService
{
    /// <summary>
    /// 取得回報人統計資料 (按回報事件數量排序)
    /// </summary>
    /// <param name="topCount">取得前 N 名，預設 10</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>回報人統計列表</returns>
    Task<List<ReporterStatisticsDto>> GetReporterStatisticsAsync(int topCount = 10, CancellationToken cancellationToken = default);

    /// <summary>
    /// 取得處理人統計資料 (按被指派事件數量排序)
    /// </summary>
    /// <param name="topCount">取得前 N 名，預設 10</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>處理人統計列表</returns>
    Task<List<AssigneeStatisticsDto>> GetAssigneeStatisticsAsync(int topCount = 10, CancellationToken cancellationToken = default);

    /// <summary>
    /// 取得問題所屬單位統計資料 (按事件數量排序)
    /// </summary>
    /// <param name="topCount">取得前 N 名，預設 10</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>單位統計列表</returns>
    Task<List<DepartmentStatisticsDto>> GetDepartmentStatisticsAsync(int topCount = 10, CancellationToken cancellationToken = default);

    /// <summary>
    /// 取得儀表板綜合統計資訊
    /// </summary>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>綜合統計資訊</returns>
    Task<DashboardSummaryDto> GetDashboardSummaryAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 清除所有儀表板快取
    /// </summary>
    void ClearAllCaches();
}
