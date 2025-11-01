using ClarityDesk.Models.DTOs;
using ClarityDesk.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace ClarityDesk.Controllers
{
    /// <summary>
    /// LINE Webhook 控制器
    /// 處理來自 LINE 平台的 Webhook 事件
    /// </summary>
    [ApiController]
    [Route("api/line/webhook")]
    public class LineWebhookController : ControllerBase
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<LineWebhookController> _logger;

        public LineWebhookController(
            IServiceScopeFactory serviceScopeFactory,
            IConfiguration configuration,
            ILogger<LineWebhookController> logger)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// LINE Webhook 端點
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Post()
        {
            try
            {
                // 讀取請求 Body
                using var reader = new StreamReader(Request.Body);
                var requestBody = await reader.ReadToEndAsync();

                // 驗證簽章
                var signature = Request.Headers["X-Line-Signature"].FirstOrDefault();
                if (string.IsNullOrEmpty(signature))
                {
                    _logger.LogWarning("Webhook 請求缺少 X-Line-Signature 標頭");
                    return BadRequest("Missing signature");
                }

                if (!ValidateSignature(requestBody, signature))
                {
                    _logger.LogWarning("Webhook 簽章驗證失敗");
                    return Unauthorized("Invalid signature");
                }

                // 解析 Webhook 事件
                var webhookRequest = JsonSerializer.Deserialize<LineWebhookRequest>(requestBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (webhookRequest == null || webhookRequest.Events == null || !webhookRequest.Events.Any())
                {
                    _logger.LogWarning("Webhook 請求不包含任何事件");
                    return Ok(); // LINE 要求即使沒有事件也要返回 200
                }

                // 異步處理事件（不阻塞回應）
                _ = Task.Run(async () =>
                {
                    try
                    {
                        // 建立新的 scope 以取得獨立的服務實例
                        using var scope = _serviceScopeFactory.CreateScope();
                        var lineMessagingService = scope.ServiceProvider.GetRequiredService<ILineMessagingService>();
                        await lineMessagingService.HandleWebhookEventAsync(webhookRequest);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "處理 Webhook 事件時發生錯誤");
                    }
                });

                // 立即返回 200 OK（LINE 要求 3 秒內回應）
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Webhook 處理失敗");
                return Ok(); // 即使發生錯誤也返回 200，避免 LINE 重試
            }
        }

        /// <summary>
        /// 驗證 LINE Webhook 簽章
        /// </summary>
        private bool ValidateSignature(string requestBody, string signature)
        {
            try
            {
                var channelSecret = _configuration["LineMessaging:ChannelSecret"];
                if (string.IsNullOrEmpty(channelSecret))
                {
                    _logger.LogError("未設定 LINE Channel Secret");
                    return false;
                }

                // 使用 HMAC-SHA256 計算簽章
                using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(channelSecret));
                var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(requestBody));
                var computedSignature = Convert.ToBase64String(hash);

                return computedSignature == signature;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "簽章驗證時發生錯誤");
                return false;
            }
        }
    }
}
