using ClarityDesk.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ClarityDesk.Infrastructure.BackgroundServices
{
    /// <summary>
    /// LINE 對話 Session 清理背景服務
    /// 定期清理過期的對話 Session
    /// </summary>
    public class LineSessionCleanupService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<LineSessionCleanupService> _logger;
        private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(1);

        public LineSessionCleanupService(
            IServiceProvider serviceProvider,
            ILogger<LineSessionCleanupService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("LINE Session 清理服務已啟動");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(_cleanupInterval, stoppingToken);

                    _logger.LogInformation("開始清理過期的 LINE Session");

                    using var scope = _serviceProvider.CreateScope();
                    var conversationService = scope.ServiceProvider.GetRequiredService<ILineConversationService>();

                    var cleanedCount = await conversationService.CleanupExpiredSessionsAsync(stoppingToken);

                    if (cleanedCount > 0)
                    {
                        _logger.LogInformation("已清理 {Count} 個過期的 LINE Session", cleanedCount);
                    }
                    else
                    {
                        _logger.LogDebug("沒有需要清理的過期 Session");
                    }
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("LINE Session 清理服務正在停止");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "清理 LINE Session 時發生錯誤");
                    // 不中斷服務,繼續下一次清理
                }
            }

            _logger.LogInformation("LINE Session 清理服務已停止");
        }
    }
}
