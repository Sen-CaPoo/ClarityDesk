using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Text;

namespace ClarityDesk.Infrastructure.Middleware
{
    /// <summary>
    /// LINE Webhook 簽章驗證中介軟體
    /// </summary>
    public class LineSignatureValidationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<LineSignatureValidationMiddleware> _logger;
        private readonly string _channelSecret;

        public LineSignatureValidationMiddleware(
            RequestDelegate next,
            ILogger<LineSignatureValidationMiddleware> logger,
            IConfiguration configuration)
        {
            _next = next;
            _logger = logger;
            _channelSecret = configuration["LineSettings:ChannelSecret"] 
                ?? throw new InvalidOperationException("LINE Channel Secret 未設定");
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // 只驗證 LINE Webhook 路徑
            if (!context.Request.Path.StartsWithSegments("/api/line/webhook"))
            {
                await _next(context);
                return;
            }

            // 取得簽章標頭
            var signature = context.Request.Headers["X-Line-Signature"].FirstOrDefault();
            if (string.IsNullOrEmpty(signature))
            {
                _logger.LogWarning("LINE Webhook 請求缺少 X-Line-Signature 標頭");
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Missing signature");
                return;
            }

            // 讀取請求 Body
            context.Request.EnableBuffering();
            using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
            var body = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;

            // 驗證簽章
            if (!ValidateSignature(body, signature))
            {
                _logger.LogWarning("LINE Webhook 簽章驗證失敗");
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Invalid signature");
                return;
            }

            _logger.LogDebug("LINE Webhook 簽章驗證成功");
            await _next(context);
        }

        private bool ValidateSignature(string body, string signature)
        {
            try
            {
                using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_channelSecret));
                var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(body));
                var computedSignature = Convert.ToBase64String(hash);
                return signature == computedSignature;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "簽章驗證時發生錯誤");
                return false;
            }
        }
    }
}
