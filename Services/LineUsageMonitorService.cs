using ClarityDesk.Data;
using ClarityDesk.Models.Enums;
using ClarityDesk.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ClarityDesk.Services
{
    /// <summary>
    /// LINE API 使用量監控服務實作
    /// </summary>
    public class LineUsageMonitorService : ILineUsageMonitorService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<LineUsageMonitorService> _logger;

        public LineUsageMonitorService(
            ApplicationDbContext context,
            IConfiguration configuration,
            ILogger<LineUsageMonitorService> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<(int UsedCount, int Limit, double UsagePercentage)> GetMonthlyUsageAsync(
            CancellationToken cancellationToken = default)
        {
            var limit = int.Parse(_configuration["LineSettings:MonthlyPushLimit"] ?? "500");
            var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

            var usedCount = await _context.LineMessageLogs
                .Where(l => l.MessageType == LineMessageType.Push
                    && l.Direction == MessageDirection.Outbound
                    && l.IsSuccess
                    && l.SentAt >= startOfMonth)
                .CountAsync(cancellationToken);

            var usagePercentage = limit > 0 ? (double)usedCount / limit * 100 : 0;

            _logger.LogInformation("當月推送用量: {Used}/{Limit} ({Percentage:F2}%)",
                usedCount, limit, usagePercentage);

            return (usedCount, limit, usagePercentage);
        }

        public async Task<IEnumerable<(DateTime Date, int SuccessCount, int FailureCount)>> GetDailyStatsAsync(
            CancellationToken cancellationToken = default)
        {
            var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30).Date;

            var stats = await _context.LineMessageLogs
                .Where(l => l.MessageType == LineMessageType.Push
                    && l.Direction == MessageDirection.Outbound
                    && l.SentAt >= thirtyDaysAgo)
                .GroupBy(l => l.SentAt.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    SuccessCount = g.Count(l => l.IsSuccess),
                    FailureCount = g.Count(l => !l.IsSuccess)
                })
                .OrderBy(s => s.Date)
                .ToListAsync(cancellationToken);

            return stats.Select(s => (s.Date, s.SuccessCount, s.FailureCount));
        }

        public async Task<bool> IsApproachingLimitAsync(CancellationToken cancellationToken = default)
        {
            var (usedCount, limit, usagePercentage) = await GetMonthlyUsageAsync(cancellationToken);

            if (usagePercentage >= 80)
            {
                await LogQuotaWarningAsync(usedCount, limit);
                return true;
            }

            return false;
        }

        public Task LogQuotaWarningAsync(int usedCount, int limit)
        {
            _logger.LogWarning("LINE 推送配額警告: 已使用 {Used}/{Limit} ({Percentage:F2}%)",
                usedCount, limit, (double)usedCount / limit * 100);

            // 未來可擴展: 發送管理員通知
            return Task.CompletedTask;
        }
    }
}
