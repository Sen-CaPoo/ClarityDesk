using ClarityDesk.Data;
using Microsoft.EntityFrameworkCore;

namespace ClarityDesk.Services;

/// <summary>
/// 背景服務：定期清理過期的 LINE 對話狀態
/// </summary>
public class ConversationCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ConversationCleanupService> _logger;
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(1); // 每小時執行一次

    public ConversationCleanupService(
        IServiceProvider serviceProvider,
        ILogger<ConversationCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ConversationCleanupService 已啟動");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupExpiredConversationsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清理過期對話狀態時發生錯誤");
            }

            await Task.Delay(_cleanupInterval, stoppingToken);
        }

        _logger.LogInformation("ConversationCleanupService 已停止");
    }

    /// <summary>
    /// 清理過期的對話狀態記錄
    /// </summary>
    private async Task CleanupExpiredConversationsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var now = DateTime.UtcNow;
        var expiredStates = await context.LineConversationStates
            .Where(cs => cs.ExpiresAt < now)
            .ToListAsync(cancellationToken);

        if (expiredStates.Any())
        {
            context.LineConversationStates.RemoveRange(expiredStates);
            await context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("已清理 {Count} 筆過期對話狀態", expiredStates.Count);

            // 清理關聯的暫存圖片
            foreach (var state in expiredStates.Where(s => !string.IsNullOrEmpty(s.ImageUrls)))
            {
                try
                {
                    var imageUrls = System.Text.Json.JsonSerializer.Deserialize<List<string>>(state.ImageUrls!);
                    if (imageUrls != null)
                    {
                        foreach (var imageUrl in imageUrls)
                        {
                            var filePath = Path.Combine("wwwroot", imageUrl.TrimStart('/').Replace("/", "\\"));
                            if (File.Exists(filePath))
                            {
                                File.Delete(filePath);
                                _logger.LogDebug("已刪除過期圖片: {FilePath}", filePath);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "清理過期圖片失敗，對話 ID: {StateId}", state.Id);
                }
            }
        }
    }
}
