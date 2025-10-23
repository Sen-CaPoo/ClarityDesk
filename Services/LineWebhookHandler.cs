using ClarityDesk.Data;
using ClarityDesk.Models.DTOs;
using ClarityDesk.Models.Enums;
using ClarityDesk.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ClarityDesk.Services
{
    /// <summary>
    /// LINE Webhook 事件處理服務實作
    /// </summary>
    public class LineWebhookHandler : ILineWebhookHandler
    {
        private readonly ILogger<LineWebhookHandler> _logger;
        private readonly ILineBindingService _lineBindingService;
        private readonly ILineMessagingService _lineMessagingService;
        private readonly ILineConversationService _lineConversationService;
        private readonly string _channelSecret;

        public LineWebhookHandler(
            ILogger<LineWebhookHandler> logger,
            ILineBindingService lineBindingService,
            ILineMessagingService lineMessagingService,
            ILineConversationService lineConversationService,
            IConfiguration configuration)
        {
            _logger = logger;
            _lineBindingService = lineBindingService;
            _lineMessagingService = lineMessagingService;
            _lineConversationService = lineConversationService;
            _channelSecret = configuration["LineSettings:ChannelSecret"] ?? throw new InvalidOperationException("LINE Channel Secret 未設定");
        }

        public async Task<int> HandleWebhookAsync(
            string webhookPayload,
            string signature,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("收到 LINE Webhook 請求");

            // 驗證簽章
            if (!ValidateSignature(webhookPayload, signature))
            {
                _logger.LogWarning("Webhook 簽章驗證失敗");
                return 401;
            }

            try
            {
                // JSON 選項: 使用 camelCase 命名策略以符合 LINE API 格式
                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                var webhook = JsonSerializer.Deserialize<LineWebhookPayload>(webhookPayload, jsonOptions);
                if (webhook?.Events == null || !webhook.Events.Any())
                {
                    _logger.LogWarning("Webhook payload 沒有事件");
                    return 200;
                }

                foreach (var evt in webhook.Events)
                {
                    await ProcessEventAsync(evt, cancellationToken);
                }

                return 200;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "處理 Webhook 時發生錯誤");
                return 500;
            }
        }

        private async Task ProcessEventAsync(LineWebhookEvent evt, CancellationToken cancellationToken)
        {
            var lineUserId = evt.Source?.UserId;
            if (string.IsNullOrEmpty(lineUserId))
            {
                _logger.LogWarning("事件沒有 userId");
                return;
            }

            switch (evt.Type?.ToLower())
            {
                case "follow":
                    await HandleFollowEventAsync(lineUserId, evt.ReplyToken ?? string.Empty, cancellationToken);
                    break;

                case "unfollow":
                    await HandleUnfollowEventAsync(lineUserId, cancellationToken);
                    break;

                case "message":
                    if (evt.Message?.Type == "text" && !string.IsNullOrEmpty(evt.Message.Text))
                    {
                        await HandleMessageEventAsync(lineUserId, evt.Message.Text, evt.ReplyToken ?? string.Empty, cancellationToken);
                    }
                    break;

                case "postback":
                    if (!string.IsNullOrEmpty(evt.Postback?.Data))
                    {
                        await HandlePostbackEventAsync(lineUserId, evt.Postback.Data, evt.ReplyToken ?? string.Empty, cancellationToken);
                    }
                    break;

                default:
                    _logger.LogInformation("未處理的事件類型: {EventType}", evt.Type);
                    break;
            }
        }

        public bool ValidateSignature(string payload, string signature)
        {
            if (string.IsNullOrEmpty(payload) || string.IsNullOrEmpty(signature))
            {
                return false;
            }

            try
            {
                using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_channelSecret));
                var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
                var computedSignature = Convert.ToBase64String(hash);
                return signature == computedSignature;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "簽章驗證時發生錯誤");
                return false;
            }
        }

        public async Task HandleFollowEventAsync(
            string lineUserId,
            string replyToken,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("處理 Follow 事件: LineUserId={LineUserId}", lineUserId);

            try
            {
                // 標記為活躍狀態 (若已綁定)
                await _lineBindingService.MarkAsActiveAsync(lineUserId, cancellationToken);
                await _lineBindingService.UpdateLastInteractionAsync(lineUserId, cancellationToken);

                // 發送歡迎訊息
                var welcomeMessage = @"🎉 歡迎使用 ClarityDesk LINE 服務!

📝 您可以透過以下指令使用我們的服務:
• 輸入「回報問題」開始建立問題回報單
• 輸入「取消」取消目前的回報流程

如需協助,請聯繫系統管理員。";

                await _lineMessagingService.ReplyMessageAsync(replyToken, new[] { welcomeMessage }, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "處理 Follow 事件時發生錯誤: LineUserId={LineUserId}", lineUserId);
            }
        }

        public async Task HandleUnfollowEventAsync(
            string lineUserId,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("處理 Unfollow 事件: LineUserId={LineUserId}", lineUserId);

            try
            {
                await _lineBindingService.MarkAsBlockedAsync(lineUserId, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "處理 Unfollow 事件時發生錯誤: LineUserId={LineUserId}", lineUserId);
            }
        }

        public async Task HandleMessageEventAsync(
            string lineUserId,
            string messageText,
            string replyToken,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("處理 Message 事件: LineUserId={LineUserId}, Message={Message}", lineUserId, messageText);

            try
            {
                await _lineBindingService.UpdateLastInteractionAsync(lineUserId, cancellationToken);

                // 檢查是否有進行中的對話
                var activeSession = await _lineConversationService.GetActiveSessionAsync(lineUserId, cancellationToken);

                // 處理特殊指令
                if (messageText.Trim() == "取消" && activeSession != null)
                {
                    await _lineConversationService.CancelConversationAsync(lineUserId, cancellationToken);
                    await _lineMessagingService.ReplyMessageAsync(replyToken, new[] { "已取消回報流程。" }, cancellationToken);
                    return;
                }

                if (messageText.Trim() == "回報問題")
                {
                    if (activeSession != null)
                    {
                        await _lineMessagingService.ReplyMessageAsync(replyToken, new[] { "您目前已有進行中的回報流程,請先完成或輸入「取消」後再開始新的回報。" }, cancellationToken);
                        return;
                    }

                    // 取得使用者資訊
                    var binding = await _lineBindingService.GetBindingByLineUserIdAsync(lineUserId, cancellationToken);
                    if (binding == null)
                    {
                        await _lineMessagingService.ReplyMessageAsync(replyToken, new[] { "您尚未綁定 ClarityDesk 帳號,請先在網頁端完成綁定。" }, cancellationToken);
                        return;
                    }

                    // 開始新的對話
                    await _lineConversationService.StartConversationAsync(lineUserId, binding.UserId, cancellationToken);
                    await _lineMessagingService.ReplyMessageAsync(replyToken, new[] { "✍️ 請輸入問題標題 (5-100 個字元):" }, cancellationToken);
                    return;
                }

                // 處理對話中的輸入
                if (activeSession != null)
                {
                    var response = await _lineConversationService.ProcessUserInputAsync(lineUserId, messageText, cancellationToken);

                    if (response.IsValid)
                    {
                        // 檢查是否完成對話
                        if (response.NextStep == ConversationStep.AwaitingConfirmation)
                        {
                            await _lineMessagingService.ReplyMessageAsync(
                                replyToken,
                                new[] { response.Message },
                                cancellationToken);
                        }
                        else
                        {
                            await _lineMessagingService.ReplyMessageAsync(
                                replyToken,
                                new[] { response.Message },
                                cancellationToken);
                        }
                    }
                    else
                    {
                        await _lineMessagingService.ReplyMessageAsync(replyToken, new[] { response.Message }, cancellationToken);
                    }
                }
                else
                {
                    // 沒有進行中的對話,提示如何開始
                    await _lineMessagingService.ReplyMessageAsync(replyToken, new[] { "請輸入「回報問題」開始建立問題回報單。" }, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "處理 Message 事件時發生錯誤: LineUserId={LineUserId}", lineUserId);
                await _lineMessagingService.ReplyMessageAsync(replyToken, new[] { "系統發生錯誤,請稍後再試。" }, cancellationToken);
            }
        }

        public async Task HandlePostbackEventAsync(
            string lineUserId,
            string postbackData,
            string replyToken,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("處理 Postback 事件: LineUserId={LineUserId}, Data={Data}", lineUserId, postbackData);

            try
            {
                await _lineBindingService.UpdateLastInteractionAsync(lineUserId, cancellationToken);

                var activeSession = await _lineConversationService.GetActiveSessionAsync(lineUserId, cancellationToken);
                if (activeSession == null)
                {
                    await _lineMessagingService.ReplyMessageAsync(replyToken, new[] { "會話已過期,請重新開始。" }, cancellationToken);
                    return;
                }

                // 處理確認送出
                if (postbackData == "confirm")
                {
                    var issueId = await _lineConversationService.CompleteConversationAsync(activeSession.Id, cancellationToken);
                    await _lineMessagingService.ReplyMessageAsync(
                        replyToken,
                        new[] { $"✅ 回報單已成功建立!\n\n回報單編號: #{issueId}\n\n感謝您的回報,我們會儘快處理。" },
                        cancellationToken);
                    return;
                }

                // 處理取消
                if (postbackData == "cancel")
                {
                    await _lineConversationService.CancelConversationAsync(lineUserId, cancellationToken);
                    await _lineMessagingService.ReplyMessageAsync(replyToken, new[] { "已取消回報流程。" }, cancellationToken);
                    return;
                }

                // 處理其他 postback (例如單位選擇)
                await _lineConversationService.ProcessUserInputAsync(lineUserId, postbackData, cancellationToken);
                var response = await _lineConversationService.ProcessUserInputAsync(lineUserId, postbackData, cancellationToken);
                await _lineMessagingService.ReplyMessageAsync(replyToken, new[] { response.Message }, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "處理 Postback 事件時發生錯誤: LineUserId={LineUserId}", lineUserId);
                await _lineMessagingService.ReplyMessageAsync(replyToken, new[] { "系統發生錯誤,請稍後再試。" }, cancellationToken);
            }
        }
    }

    // Webhook Payload 模型 (用於反序列化)
    internal class LineWebhookPayload
    {
        public string? Destination { get; set; }
        public List<LineWebhookEvent>? Events { get; set; }
    }

    internal class LineWebhookEvent
    {
        public string? Type { get; set; }
        public LineEventSource? Source { get; set; }
        public string? ReplyToken { get; set; }
        public LineMessage? Message { get; set; }
        public LinePostback? Postback { get; set; }
    }

    internal class LineEventSource
    {
        public string? Type { get; set; }
        public string? UserId { get; set; }
    }

    internal class LineMessage
    {
        public string? Id { get; set; }
        public string? Type { get; set; }
        public string? Text { get; set; }
    }

    internal class LinePostback
    {
        public string? Data { get; set; }
    }
}
