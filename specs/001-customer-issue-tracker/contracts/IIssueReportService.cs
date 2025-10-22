using ClarityDesk.Models.DTOs;
using ClarityDesk.Models.Entities;
using ClarityDesk.Models.Enums;

namespace ClarityDesk.Services.Interfaces;

/// <summary>
/// 回報單管理服務介面
/// </summary>
public interface IIssueReportService
{
    /// <summary>
    /// 建立新回報單
    /// </summary>
    /// <param name="dto">回報單建立資料</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>建立的回報單 ID</returns>
    Task<int> CreateIssueReportAsync(CreateIssueReportDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新回報單
    /// </summary>
    /// <param name="id">回報單 ID</param>
    /// <param name="dto">回報單更新資料</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>是否更新成功</returns>
    Task<bool> UpdateIssueReportAsync(int id, UpdateIssueReportDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// 刪除回報單
    /// </summary>
    /// <param name="id">回報單 ID</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>是否刪除成功</returns>
    Task<bool> DeleteIssueReportAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根據 ID 取得回報單詳細資料
    /// </summary>
    /// <param name="id">回報單 ID</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>回報單詳細資料</returns>
    Task<IssueReportDto?> GetIssueReportByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 取得回報單列表 (支援篩選與分頁)
    /// </summary>
    /// <param name="filter">篩選條件</param>
    /// <param name="page">頁碼 (從 1 開始)</param>
    /// <param name="pageSize">每頁筆數</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>分頁後的回報單列表</returns>
    Task<PagedResult<IssueReportDto>> GetIssueReportsAsync(
        IssueFilterDto filter,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 取得回報單統計資訊
    /// </summary>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>統計資訊</returns>
    Task<IssueStatisticsDto> GetIssueStatisticsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新回報單狀態
    /// </summary>
    /// <param name="id">回報單 ID</param>
    /// <param name="newStatus">新狀態</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>是否更新成功</returns>
    Task<bool> UpdateIssueStatusAsync(int id, IssueStatus newStatus, CancellationToken cancellationToken = default);

    /// <summary>
    /// 指派回報單給使用者
    /// </summary>
    /// <param name="id">回報單 ID</param>
    /// <param name="assignedUserId">指派使用者 ID</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>是否指派成功</returns>
    Task<bool> AssignIssueToUserAsync(int id, int assignedUserId, CancellationToken cancellationToken = default);
}
