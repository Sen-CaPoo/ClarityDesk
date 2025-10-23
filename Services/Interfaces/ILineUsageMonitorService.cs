using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ClarityDesk.Services.Interfaces
{
    /// <summary>
    /// LINE API 使用量監控服務介面
    /// </summary>
    public interface ILineUsageMonitorService
    {
        /// <summary>
        /// 取得當月推送訊息用量統計
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>用量統計 (已使用數量, 限制數量, 使用率百分比)</returns>
        Task<(int UsedCount, int Limit, double UsagePercentage)> GetMonthlyUsageAsync(
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 取得每日推送訊息統計 (最近 30 天)
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>每日統計 (日期, 成功數, 失敗數)</returns>
        Task<IEnumerable<(DateTime Date, int SuccessCount, int FailureCount)>> GetDailyStatsAsync(
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 檢查是否接近配額限制 (≥ 80%)
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>true: 接近限制需警告; false: 使用量正常</returns>
        Task<bool> IsApproachingLimitAsync(
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 記錄配額警告日誌
        /// </summary>
        /// <param name="usedCount">已使用數量</param>
        /// <param name="limit">配額限制</param>
        Task LogQuotaWarningAsync(int usedCount, int limit);
    }
}
