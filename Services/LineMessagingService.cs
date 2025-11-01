using ClarityDesk.Data;
using ClarityDesk.Models.DTOs;
using ClarityDesk.Models.Entities;
using ClarityDesk.Models.Enums;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ClarityDesk.Services
{
    /// <summary>
    /// LINE Messaging 服務實作
    /// </summary>
    public class LineMessagingService : Interfaces.ILineMessagingService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<LineMessagingService> _logger;
        private readonly string _channelAccessToken;
        private readonly string _channelSecret;
        private readonly string _baseUrl;

        public LineMessagingService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ApplicationDbContext context,
            ILogger<LineMessagingService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _context = context;
            _logger = logger;

            _channelAccessToken = configuration["LineMessaging:ChannelAccessToken"] ?? "";
            _channelSecret = configuration["LineMessaging:ChannelSecret"] ?? "";
            _baseUrl = configuration["LineMessaging:BaseUrl"] ?? "https://claritydesk.example.com";
        }

        /// <summary>
        /// 發送 LINE Push Message
        /// </summary>
        public async Task<bool> PushMessageAsync(string lineUserId, object message)
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient("LineMessagingAPI");
                httpClient.DefaultRequestHeaders.Clear();
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_channelAccessToken}");

                var payload = new
                {
                    to = lineUserId,
                    messages = new[] { message }
                };

                var response = await httpClient.PostAsJsonAsync("message/push", payload);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("LINE Push Message 失敗: {StatusCode} - {Error}", response.StatusCode, errorContent);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "發送 LINE Push Message 時發生錯誤: {LineUserId}", lineUserId);
                return false;
            }
        }

        /// <summary>
        /// 推送新問題回報單通知（Flex Message 格式）
        /// </summary>
        public async Task<bool> PushNewIssueNotificationAsync(IssueReportDto issue, string targetLineUserId)
        {
            try
            {
                var detailsUrl = $"{_baseUrl}/Issues/Details/{issue.Id}";
                var flexMessage = BuildNewIssueFlexMessage(issue, detailsUrl);

                var success = await PushMessageWithRetryAsync(targetLineUserId, flexMessage, issue.Id, "NewIssue");

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "推送新問題通知時發生錯誤: {IssueId}", issue.Id);
                return false;
            }
        }

        /// <summary>
        /// 推送問題狀態變更通知
        /// </summary>
        public async Task<bool> PushStatusChangedNotificationAsync(IssueReportDto issue, string oldStatus, string newStatus, string targetLineUserId)
        {
            try
            {
                var detailsUrl = $"{_baseUrl}/Issues/Details/{issue.Id}";
                var message = BuildStatusChangedMessage(issue, oldStatus, newStatus, detailsUrl);

                var success = await PushMessageWithRetryAsync(targetLineUserId, message, issue.Id, "StatusChanged");

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "推送狀態變更通知時發生錯誤: {IssueId}", issue.Id);
                return false;
            }
        }

        /// <summary>
        /// 推送問題指派變更通知（Flex Message 格式）
        /// </summary>
        public async Task<bool> PushAssignmentChangedNotificationAsync(IssueReportDto issue, string newAssigneeName, string targetLineUserId)
        {
            try
            {
                var detailsUrl = $"{_baseUrl}/Issues/Details/{issue.Id}";
                var flexMessage = BuildAssignmentChangedFlexMessage(issue, newAssigneeName, detailsUrl);

                var success = await PushMessageWithRetryAsync(targetLineUserId, flexMessage, issue.Id, "AssignmentChanged");

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "推送指派變更通知時發生錯誤: {IssueId}", issue.Id);
                return false;
            }
        }

        /// <summary>
        /// 處理 LINE Webhook 事件
        /// </summary>
        public async Task HandleWebhookEventAsync(LineWebhookRequest webhookRequest)
        {
            foreach (var evt in webhookRequest.Events)
            {
                try
                {
                    var lineUserId = evt.Source?.UserId;
                    if (string.IsNullOrEmpty(lineUserId))
                    {
                        _logger.LogWarning("Webhook 事件缺少 LineUserId");
                        continue;
                    }

                    switch (evt.Type.ToLowerInvariant())
                    {
                        case "message":
                            await HandleMessageEventAsync(evt, lineUserId);
                            break;

                        case "postback":
                            await HandlePostbackEventAsync(evt, lineUserId);
                            break;

                        default:
                            _logger.LogInformation("未處理的事件類型: {EventType}", evt.Type);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "處理 Webhook 事件時發生錯誤: {EventType}", evt.Type);
                }
            }
        }

        /// <summary>
        /// 回覆 LINE 訊息
        /// </summary>
        public async Task<bool> ReplyMessageAsync(string replyToken, object message)
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient("LineMessagingAPI");
                httpClient.DefaultRequestHeaders.Clear();
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_channelAccessToken}");

                var payload = new
                {
                    replyToken = replyToken,
                    messages = new[] { message }
                };

                var response = await httpClient.PostAsJsonAsync("message/reply", payload);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("LINE Reply Message 失敗: {StatusCode} - {Error}", response.StatusCode, errorContent);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "回覆 LINE 訊息時發生錯誤: {ReplyToken}", replyToken);
                return false;
            }
        }

        #region Private Helper Methods

        /// <summary>
        /// 帶重試機制的推送訊息
        /// </summary>
        private async Task<bool> PushMessageWithRetryAsync(string lineUserId, object message, int issueReportId, string messageType, int maxRetries = 3)
        {
            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                try
                {
                    var success = await PushMessageAsync(lineUserId, message);

                    // 記錄推送日誌
                    var pushLog = new LinePushLog
                    {
                        IssueReportId = issueReportId,
                        LineUserId = lineUserId,
                        MessageType = messageType,
                        Status = success ? LinePushStatus.Success : LinePushStatus.Failed,
                        RetryCount = attempt,
                        ErrorMessage = success ? null : "推送失敗",
                        PushedAt = DateTime.UtcNow
                    };

                    _context.LinePushLogs.Add(pushLog);
                    await _context.SaveChangesAsync();

                    if (success)
                    {
                        _logger.LogInformation("LINE 推送成功: {MessageType} -> {LineUserId} (Issue: {IssueId})", messageType, lineUserId, issueReportId);
                        return true;
                    }

                    // 指數退避
                    if (attempt < maxRetries - 1)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt))); // 1s, 2s, 4s
                        _logger.LogInformation("LINE 推送重試 {Attempt}/{MaxRetries}", attempt + 1, maxRetries);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "LINE 推送失敗 (嘗試 {Attempt}/{MaxRetries})", attempt + 1, maxRetries);

                    if (attempt == maxRetries - 1)
                    {
                        // 最後一次重試失敗，記錄錯誤日誌
                        var pushLog = new LinePushLog
                        {
                            IssueReportId = issueReportId,
                            LineUserId = lineUserId,
                            MessageType = messageType,
                            Status = LinePushStatus.Failed,
                            RetryCount = attempt,
                            ErrorMessage = ex.Message,
                            PushedAt = DateTime.UtcNow
                        };

                        _context.LinePushLogs.Add(pushLog);
                        await _context.SaveChangesAsync();
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// 構建新問題 Flex Message
        /// </summary>
        private object BuildNewIssueFlexMessage(IssueReportDto issue, string detailsUrl)
        {
            var (priorityEmoji, priorityColor) = GetPriorityDisplay(issue.Priority.ToString());

            return new
            {
                type = "flex",
                altText = $"新問題回報單通知 - IR-{issue.Id}",
                contents = new
                {
                    type = "bubble",
                    header = new
                    {
                        type = "box",
                        layout = "vertical",
                        contents = new[]
                        {
                            new { type = "text", text = "🔔 新問題回報單", weight = "bold", size = "lg", color = "#1DB446" }
                        }
                    },
                    body = new
                    {
                        type = "box",
                        layout = "vertical",
                        spacing = "sm",
                        contents = new object[]
                        {
                            CreateField("編號", $"IR-{issue.Id}", bold: true),
                            CreateField("標題", issue.Title, wrap: true),
                            CreateField("緊急程度", $"{priorityEmoji} {GetPriorityText(issue.Priority.ToString())}", color: priorityColor),
                            new { type = "separator", margin = "md" },
                            CreateField("單位", string.Join(", ", issue.DepartmentNames)),
                            CreateField("聯絡人", issue.CustomerName),
                            CreateField("電話", issue.CustomerPhone),
                            CreateField("日期", issue.RecordDate.ToString("yyyy-MM-dd")),
                            CreateField("回報人", issue.AssignedUserName ?? "未指派")
                        }
                    },
                    footer = new
                    {
                        type = "box",
                        layout = "vertical",
                        contents = new[]
                        {
                            new
                            {
                                type = "button",
                                action = new { type = "uri", label = "查看詳細內容", uri = detailsUrl },
                                style = "primary",
                                color = "#1DB446"
                            }
                        }
                    }
                }
            };
        }

        /// <summary>
        /// 構建狀態變更訊息（文字格式）
        /// </summary>
        private object BuildStatusChangedMessage(IssueReportDto issue, string oldStatus, string newStatus, string detailsUrl)
        {
            var statusEmoji = newStatus == "Completed" ? "✅" : "⏳";

            return new
            {
                type = "text",
                text = $"{statusEmoji} 問題回報單狀態更新\n\n" +
                       $"回報單編號：IR-{issue.Id}\n" +
                       $"標題：{issue.Title}\n" +
                       $"舊狀態：{GetStatusText(oldStatus)}\n" +
                       $"新狀態：{GetStatusText(newStatus)}\n\n" +
                       $"查看詳細內容：\n{detailsUrl}"
            };
        }

        /// <summary>
        /// 構建指派變更 Flex Message
        /// </summary>
        private object BuildAssignmentChangedFlexMessage(IssueReportDto issue, string newAssigneeName, string detailsUrl)
        {
            var (priorityEmoji, priorityColor) = GetPriorityDisplay(issue.Priority.ToString());

            return new
            {
                type = "flex",
                altText = $"問題已指派給您 - IR-{issue.Id}",
                contents = new
                {
                    type = "bubble",
                    header = new
                    {
                        type = "box",
                        layout = "vertical",
                        contents = new[]
                        {
                            new { type = "text", text = "👤 問題已指派給您", weight = "bold", size = "lg", color = "#0084FF" }
                        }
                    },
                    body = new
                    {
                        type = "box",
                        layout = "vertical",
                        spacing = "sm",
                        contents = new object[]
                        {
                            CreateField("編號", $"IR-{issue.Id}", bold: true),
                            CreateField("標題", issue.Title, wrap: true),
                            CreateField("緊急程度", $"{priorityEmoji} {GetPriorityText(issue.Priority.ToString())}", color: priorityColor),
                            new { type = "separator", margin = "md" },
                            CreateField("單位", string.Join(", ", issue.DepartmentNames)),
                            CreateField("聯絡人", issue.CustomerName)
                        }
                    },
                    footer = new
                    {
                        type = "box",
                        layout = "vertical",
                        contents = new[]
                        {
                            new
                            {
                                type = "button",
                                action = new { type = "uri", label = "查看詳細內容", uri = detailsUrl },
                                style = "primary",
                                color = "#0084FF"
                            }
                        }
                    }
                }
            };
        }

        /// <summary>
        /// 建立 Flex Message 欄位
        /// </summary>
        private object CreateField(string label, string value, bool bold = false, bool wrap = false, string? color = null)
        {
            var textProperties = new Dictionary<string, object>
            {
                { "type", "text" },
                { "text", value },
                { "size", "sm" },
                { "flex", 5 }
            };

            if (bold)
                textProperties.Add("weight", "bold");
            
            if (wrap)
                textProperties.Add("wrap", true);
            
            if (!string.IsNullOrEmpty(color))
                textProperties.Add("color", color);

            return new
            {
                type = "box",
                layout = "baseline",
                contents = new object[]
                {
                    new { type = "text", text = label, size = "sm", color = "#999999", flex = 2 },
                    textProperties
                }
            };
        }

        /// <summary>
        /// 取得優先級顯示
        /// </summary>
        private (string emoji, string color) GetPriorityDisplay(string priority)
        {
            return priority switch
            {
                "High" => ("🔴", "#FF0000"),
                "Medium" => ("🟡", "#FFA500"),
                "Low" => ("🟢", "#008000"),
                _ => ("", "#000000")
            };
        }

        /// <summary>
        /// 取得優先級文字
        /// </summary>
        private string GetPriorityText(string priority)
        {
            return priority switch
            {
                "High" => "高",
                "Medium" => "中",
                "Low" => "低",
                _ => priority
            };
        }

        /// <summary>
        /// 取得狀態文字
        /// </summary>
        private string GetStatusText(string status)
        {
            return status switch
            {
                "Pending" => "待處理",
                "Completed" => "已完成",
                _ => status
            };
        }

        /// <summary>
        /// 處理訊息事件
        /// </summary>
        private async Task HandleMessageEventAsync(LineMessageDto evt, string lineUserId)
        {
            if (evt.Message?.Type == "text")
            {
                await HandleTextMessageAsync(evt, lineUserId);
            }
            else if (evt.Message?.Type == "image")
            {
                await HandleImageMessageAsync(evt, lineUserId);
            }
        }

        /// <summary>
        /// 處理文字訊息（對話流程）
        /// </summary>
        private async Task HandleTextMessageAsync(LineMessageDto evt, string lineUserId)
        {
            var text = evt.Message?.Text?.Trim();
            if (string.IsNullOrEmpty(text))
                return;

            _logger.LogInformation("收到文字訊息: {Text} from {LineUserId}", text, lineUserId);

            // 檢查使用者是否已綁定
            var binding = await _context.LineBindings
                .Where(lb => lb.LineUserId == lineUserId && lb.IsActive)
                .FirstOrDefaultAsync();

            if (binding == null)
            {
                await ReplyMessageAsync(evt.ReplyToken, new
                {
                    type = "text",
                    text = $"⚠️ 您尚未綁定系統帳號\n\n請先登入 ClarityDesk 網站完成帳號綁定：\n{_baseUrl}/Account/LineBinding"
                });
                return;
            }

            // 檢查是否為取消指令
            if (text == "取消")
            {
                var existingState = await _context.LineConversationStates
                    .Where(cs => cs.LineUserId == lineUserId)
                    .FirstOrDefaultAsync();

                if (existingState != null)
                {
                    _context.LineConversationStates.Remove(existingState);
                    await _context.SaveChangesAsync();

                    await ReplyMessageAsync(evt.ReplyToken, new
                    {
                        type = "text",
                        text = "❌ 已取消問題回報流程"
                    });
                }
                return;
            }

            // 檢查是否為啟動回報流程指令
            if (text == "回報問題")
            {
                // 檢查是否已有進行中的對話
                var existingState = await _context.LineConversationStates
                    .Where(cs => cs.LineUserId == lineUserId)
                    .FirstOrDefaultAsync();

                if (existingState != null)
                {
                    _context.LineConversationStates.Remove(existingState);
                }

                // 建立新的對話狀態
                var newState = new LineConversationState
                {
                    LineUserId = lineUserId,
                    UserId = binding.UserId,
                    CurrentStep = ConversationStep.AskTitle,
                    ExpiresAt = DateTime.UtcNow.AddHours(24)
                };

                _context.LineConversationStates.Add(newState);
                await _context.SaveChangesAsync();

                await ReplyMessageAsync(evt.ReplyToken, new
                {
                    type = "text",
                    text = "📝 開始問題回報流程\n\n請輸入問題標題："
                });
                return;
            }

            // 處理對話中的訊息
            await ProcessConversationStepAsync(evt.ReplyToken, lineUserId, text);
        }

        /// <summary>
        /// 處理圖片訊息
        /// </summary>
        private async Task HandleImageMessageAsync(LineMessageDto evt, string lineUserId)
        {
            _logger.LogInformation("收到圖片訊息: {MessageId} from {LineUserId}", evt.Message?.Id, lineUserId);

            var conversationState = await _context.LineConversationStates
                .Where(cs => cs.LineUserId == lineUserId)
                .FirstOrDefaultAsync();

            if (conversationState == null || conversationState.CurrentStep != ConversationStep.AskImages)
            {
                await ReplyMessageAsync(evt.ReplyToken, new
                {
                    type = "text",
                    text = "請先輸入『回報問題』開始回報流程"
                });
                return;
            }

            // 檢查圖片數量限制
            var imageUrls = string.IsNullOrEmpty(conversationState.ImageUrls)
                ? new List<string>()
                : JsonSerializer.Deserialize<List<string>>(conversationState.ImageUrls) ?? new List<string>();

            if (imageUrls.Count >= 3)
            {
                await ReplyMessageAsync(evt.ReplyToken, new
                {
                    type = "text",
                    text = "❌ 已達圖片上傳上限（3 張），請輸入「跳過」繼續"
                });
                return;
            }

            // 下載並儲存圖片
            try
            {
                var imageUrl = await DownloadLineImageAsync(evt.Message!.Id);
                if (imageUrl != null)
                {
                    imageUrls.Add(imageUrl);
                    conversationState.ImageUrls = JsonSerializer.Serialize(imageUrls);
                    await _context.SaveChangesAsync();

                    await ReplyMessageAsync(evt.ReplyToken, new
                    {
                        type = "text",
                        text = $"✅ 已收到圖片 {imageUrls.Count}/3\n\n可繼續上傳或輸入「跳過」完成"
                    });
                }
                else
                {
                    await ReplyMessageAsync(evt.ReplyToken, new
                    {
                        type = "text",
                        text = "❌ 圖片接收失敗，請重新上傳或輸入「跳過」繼續"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "下載 LINE 圖片時發生錯誤: {MessageId}", evt.Message.Id);
                await ReplyMessageAsync(evt.ReplyToken, new
                {
                    type = "text",
                    text = "❌ 圖片接收失敗，請重新上傳或輸入「跳過」繼續"
                });
            }
        }

        /// <summary>
        /// 處理 Postback 事件（按鈕點擊）
        /// </summary>
        private async Task HandlePostbackEventAsync(LineMessageDto evt, string lineUserId)
        {
            var postbackData = evt.Postback?.Data;
            if (string.IsNullOrEmpty(postbackData))
                return;

            _logger.LogInformation("收到 Postback: {Data} from {LineUserId}", postbackData, lineUserId);

            var conversationState = await _context.LineConversationStates
                .Where(cs => cs.LineUserId == lineUserId)
                .FirstOrDefaultAsync();

            if (conversationState == null)
            {
                await ReplyMessageAsync(evt.ReplyToken, new
                {
                    type = "text",
                    text = "對話已過期，請重新開始"
                });
                return;
            }

            var data = ParsePostbackData(postbackData);

            if (data.TryGetValue("action", out var action))
            {
                switch (action)
                {
                    case "select_department":
                        if (data.TryGetValue("id", out var deptId) && int.TryParse(deptId, out var departmentId))
                        {
                            conversationState.DepartmentId = departmentId;
                            conversationState.CurrentStep = ConversationStep.AskPriority;
                            await _context.SaveChangesAsync();

                            await ReplyMessageAsync(evt.ReplyToken, BuildPriorityQuickReply());
                        }
                        break;

                    case "select_priority":
                        if (data.TryGetValue("value", out var priority))
                        {
                            conversationState.Priority = priority;
                            conversationState.CurrentStep = ConversationStep.AskCustomerName;
                            await _context.SaveChangesAsync();

                            await ReplyMessageAsync(evt.ReplyToken, new
                            {
                                type = "text",
                                text = $"✅ 緊急程度：{GetPriorityText(priority)}\n\n請輸入聯絡人姓名："
                            });
                        }
                        break;

                    case "confirm":
                        await HandleConfirmSubmissionAsync(evt.ReplyToken, lineUserId, conversationState);
                        break;

                    case "cancel":
                        _context.LineConversationStates.Remove(conversationState);
                        await _context.SaveChangesAsync();

                        await ReplyMessageAsync(evt.ReplyToken, new
                        {
                            type = "text",
                            text = "❌ 已取消問題回報流程"
                        });
                        break;
                }
            }
        }

        /// <summary>
        /// 處理對話步驟
        /// </summary>
        private async Task ProcessConversationStepAsync(string replyToken, string lineUserId, string userInput)
        {
            var conversationState = await _context.LineConversationStates
                .Where(cs => cs.LineUserId == lineUserId)
                .FirstOrDefaultAsync();

            if (conversationState == null)
            {
                await ReplyMessageAsync(replyToken, new
                {
                    type = "text",
                    text = "⚠️ 無法辨識的指令\n\n請輸入「回報問題」開始回報流程"
                });
                return;
            }

            // 檢查是否過期
            if (conversationState.ExpiresAt < DateTime.UtcNow)
            {
                _context.LineConversationStates.Remove(conversationState);
                await _context.SaveChangesAsync();

                await ReplyMessageAsync(replyToken, new
                {
                    type = "text",
                    text = "⏱️ 對話已過期，請重新開始"
                });
                return;
            }

            switch (conversationState.CurrentStep)
            {
                case ConversationStep.AskTitle:
                    conversationState.Title = userInput;
                    conversationState.CurrentStep = ConversationStep.AskContent;
                    await _context.SaveChangesAsync();

                    await ReplyMessageAsync(replyToken, new
                    {
                        type = "text",
                        text = $"✅ 問題標題：{userInput}\n\n請輸入問題詳細內容："
                    });
                    break;

                case ConversationStep.AskContent:
                    conversationState.Content = userInput;
                    conversationState.CurrentStep = ConversationStep.AskDepartment;
                    await _context.SaveChangesAsync();

                    await ReplyMessageAsync(replyToken, await BuildDepartmentQuickReplyAsync());
                    break;

                case ConversationStep.AskCustomerName:
                    conversationState.CustomerName = userInput;
                    conversationState.CurrentStep = ConversationStep.AskCustomerPhone;
                    await _context.SaveChangesAsync();

                    await ReplyMessageAsync(replyToken, new
                    {
                        type = "text",
                        text = $"✅ 聯絡人：{userInput}\n\n請輸入連絡電話："
                    });
                    break;

                case ConversationStep.AskCustomerPhone:
                    conversationState.CustomerPhone = userInput;
                    conversationState.CurrentStep = ConversationStep.AskImages;
                    await _context.SaveChangesAsync();

                    await ReplyMessageAsync(replyToken, new
                    {
                        type = "text",
                        text = $"✅ 連絡電話：{userInput}\n\n是否需要上傳圖片？\n（可直接傳送圖片，最多 3 張，或輸入「跳過」繼續）"
                    });
                    break;

                case ConversationStep.AskImages:
                    if (userInput == "跳過")
                    {
                        conversationState.CurrentStep = ConversationStep.Confirm;
                        await _context.SaveChangesAsync();

                        await ReplyMessageAsync(replyToken, await BuildConfirmMessageAsync(conversationState));
                    }
                    break;

                default:
                    await ReplyMessageAsync(replyToken, new
                    {
                        type = "text",
                        text = "⚠️ 對話流程錯誤，請重新開始"
                    });
                    break;
            }
        }

        /// <summary>
        /// 建立單位選擇快速回覆
        /// </summary>
        private async Task<object> BuildDepartmentQuickReplyAsync()
        {
            var departments = await _context.Departments
                .Where(d => d.IsActive)
                .OrderBy(d => d.Name)
                .Take(13) // LINE 快速回覆最多 13 個選項
                .ToListAsync();

            var quickReplyItems = departments.Select(d => new
            {
                type = "action",
                action = new
                {
                    type = "postback",
                    label = d.Name,
                    data = $"action=select_department&id={d.Id}"
                }
            }).ToList();

            return new
            {
                type = "text",
                text = "請選擇問題所屬單位：",
                quickReply = new
                {
                    items = quickReplyItems
                }
            };
        }

        /// <summary>
        /// 建立緊急程度選擇快速回覆
        /// </summary>
        private object BuildPriorityQuickReply()
        {
            return new
            {
                type = "text",
                text = "請選擇問題緊急程度：",
                quickReply = new
                {
                    items = new[]
                    {
                        new
                        {
                            type = "action",
                            action = new
                            {
                                type = "postback",
                                label = "🔴 高",
                                data = "action=select_priority&value=High"
                            }
                        },
                        new
                        {
                            type = "action",
                            action = new
                            {
                                type = "postback",
                                label = "🟡 中",
                                data = "action=select_priority&value=Medium"
                            }
                        },
                        new
                        {
                            type = "action",
                            action = new
                            {
                                type = "postback",
                                label = "🟢 低",
                                data = "action=select_priority&value=Low"
                            }
                        }
                    }
                }
            };
        }

        /// <summary>
        /// 建立確認訊息
        /// </summary>
        private async Task<object> BuildConfirmMessageAsync(LineConversationState state)
        {
            var department = state.DepartmentId.HasValue
                ? (await _context.Departments.FindAsync(state.DepartmentId.Value))?.Name ?? "未知"
                : "未選擇";

            var imageCount = string.IsNullOrEmpty(state.ImageUrls)
                ? 0
                : JsonSerializer.Deserialize<List<string>>(state.ImageUrls)?.Count ?? 0;

            var priorityText = GetPriorityText(state.Priority ?? "Medium");
            var (priorityEmoji, _) = GetPriorityDisplay(state.Priority ?? "Medium");

            return new
            {
                type = "text",
                text = $"📋 請確認以下資訊：\n\n" +
                       $"標題：{state.Title}\n" +
                       $"內容：{state.Content}\n" +
                       $"單位：{department}\n" +
                       $"緊急程度：{priorityEmoji} {priorityText}\n" +
                       $"聯絡人：{state.CustomerName}\n" +
                       $"電話：{state.CustomerPhone}\n" +
                       $"圖片：{imageCount} 張\n\n" +
                       $"請點擊下方按鈕確認送出：",
                quickReply = new
                {
                    items = new[]
                    {
                        new
                        {
                            type = "action",
                            action = new
                            {
                                type = "postback",
                                label = "✅ 確認送出",
                                data = "action=confirm"
                            }
                        },
                        new
                        {
                            type = "action",
                            action = new
                            {
                                type = "postback",
                                label = "❌ 取消",
                                data = "action=cancel"
                            }
                        }
                    }
                }
            };
        }

        /// <summary>
        /// 處理確認送出
        /// </summary>
        private async Task HandleConfirmSubmissionAsync(string replyToken, string lineUserId, LineConversationState state)
        {
            try
            {
                // 建立問題回報單
                var issue = new IssueReport
                {
                    Title = state.Title ?? "未命名",
                    Content = state.Content ?? "",
                    Priority = Enum.Parse<PriorityLevel>(state.Priority ?? "Medium"),
                    Status = IssueStatus.Pending,
                    CustomerName = state.CustomerName ?? "",
                    CustomerPhone = state.CustomerPhone ?? "",
                    RecordDate = DateTime.UtcNow,
                    AssignedUserId = await GetDefaultAssigneeForDepartmentAsync(state.DepartmentId) ?? 1 // Fallback to user ID 1 (admin)
                };

                _context.IssueReports.Add(issue);
                await _context.SaveChangesAsync();

                // 建立單位指派
                if (state.DepartmentId.HasValue)
                {
                    var assignment = new DepartmentAssignment
                    {
                        IssueReportId = issue.Id,
                        DepartmentId = state.DepartmentId.Value,
                        AssignedAt = DateTime.UtcNow
                    };

                    _context.DepartmentAssignments.Add(assignment);
                    await _context.SaveChangesAsync();
                }

                // 移動圖片到最終位置
                if (!string.IsNullOrEmpty(state.ImageUrls))
                {
                    var imageUrls = JsonSerializer.Deserialize<List<string>>(state.ImageUrls);
                    if (imageUrls != null && imageUrls.Any())
                    {
                        var finalImageUrls = new List<string>();
                        var issueImagePath = Path.Combine("wwwroot", "uploads", "issues", issue.Id.ToString());
                        Directory.CreateDirectory(issueImagePath);

                        foreach (var tempUrl in imageUrls)
                        {
                            try
                            {
                                // tempUrl 格式: /uploads/line-images/123456_msgId.jpg
                                var tempFileName = Path.GetFileName(tempUrl);
                                var tempFilePath = Path.Combine("wwwroot", tempUrl.TrimStart('/').Replace("/", "\\"));

                                if (File.Exists(tempFilePath))
                                {
                                    var finalFileName = $"{DateTime.UtcNow.Ticks}_{Path.GetRandomFileName()}.jpg";
                                    var finalFilePath = Path.Combine(issueImagePath, finalFileName);

                                    File.Move(tempFilePath, finalFilePath);
                                    finalImageUrls.Add($"/uploads/issues/{issue.Id}/{finalFileName}");
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "移動圖片失敗: {TempUrl}", tempUrl);
                            }
                        }

                        _logger.LogInformation("問題 {IssueId} 成功移動 {Count}/{Total} 張圖片",
                            issue.Id, finalImageUrls.Count, imageUrls.Count);
                    }
                }

                // 清除對話狀態
                _context.LineConversationStates.Remove(state);
                await _context.SaveChangesAsync();

                // 取得指派人員名稱
                var assigneeName = issue.AssignedUserId > 0
                    ? (await _context.Users.FindAsync(issue.AssignedUserId))?.DisplayName ?? "系統自動指派"
                    : "尚未指派";

                // 回覆成功訊息
                var detailsUrl = $"{_baseUrl}/Issues/Details/{issue.Id}";
                await ReplyMessageAsync(replyToken, new
                {
                    type = "flex",
                    altText = $"問題回報單已建立 - IR-{issue.Id}",
                    contents = new
                    {
                        type = "bubble",
                        body = new
                        {
                            type = "box",
                            layout = "vertical",
                            spacing = "md",
                            contents = new object[]
                            {
                                new
                                {
                                    type = "text",
                                    text = "✅ 問題回報單已建立！",
                                    weight = "bold",
                                    size = "lg",
                                    color = "#1DB446"
                                },
                                CreateField("回報單編號", $"IR-{issue.Id}", bold: true),
                                CreateField("指派處理人員", assigneeName)
                            }
                        },
                        footer = new
                        {
                            type = "box",
                            layout = "vertical",
                            contents = new[]
                            {
                                new
                                {
                                    type = "button",
                                    action = new { type = "uri", label = "查看詳細內容", uri = detailsUrl },
                                    style = "primary",
                                    color = "#1DB446"
                                }
                            }
                        }
                    }
                });

                _logger.LogInformation("成功從 LINE 建立問題回報單 {IssueId}", issue.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "建立問題回報單時發生錯誤");
                await ReplyMessageAsync(replyToken, new
                {
                    type = "text",
                    text = "❌ 建立失敗，請稍後再試或聯繫系統管理員"
                });
            }
        }

        /// <summary>
        /// 下載 LINE 圖片
        /// </summary>
        private async Task<string?> DownloadLineImageAsync(string messageId)
        {
            try
            {
                // 驗證 Token 是否配置
                if (string.IsNullOrEmpty(_channelAccessToken))
                {
                    _logger.LogError("LINE Channel Access Token 未設定");
                    return null;
                }

                // 使用 LINE Content API (api-data.line.me) 下載圖片
                var httpClient = _httpClientFactory.CreateClient("LineContentAPI");
                httpClient.DefaultRequestHeaders.Clear();
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_channelAccessToken}");

                var endpoint = $"message/{messageId}/content";
                _logger.LogInformation("開始下載 LINE 圖片: {Endpoint}, MessageId: {MessageId}", endpoint, messageId);

                var response = await httpClient.GetAsync(endpoint);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("下載 LINE 圖片失敗: StatusCode={StatusCode}, MessageId={MessageId}, Error={Error}", 
                        response.StatusCode, messageId, errorBody);
                    return null;
                }

                var imageData = await response.Content.ReadAsByteArrayAsync();
                _logger.LogInformation("成功下載 LINE 圖片: MessageId={MessageId}, Size={Size} bytes", messageId, imageData.Length);

                var uploadPath = _configuration["LineMessaging:ImageUploadPath"] ?? "wwwroot/uploads/line-images";
                var fileName = $"{DateTime.UtcNow.Ticks}_{messageId}.jpg";
                var filePath = Path.Combine(uploadPath, fileName);

                Directory.CreateDirectory(uploadPath);
                await File.WriteAllBytesAsync(filePath, imageData);

                _logger.LogInformation("LINE 圖片已儲存: {FilePath}", filePath);

                return $"/uploads/line-images/{fileName}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "下載 LINE 圖片時發生錯誤: {MessageId}", messageId);
                return null;
            }
        }

        /// <summary>
        /// 解析 Postback 資料
        /// </summary>
        private Dictionary<string, string> ParsePostbackData(string data)
        {
            var result = new Dictionary<string, string>();
            var pairs = data.Split('&');

            foreach (var pair in pairs)
            {
                var keyValue = pair.Split('=');
                if (keyValue.Length == 2)
                {
                    result[keyValue[0]] = keyValue[1];
                }
            }

            return result;
        }

        /// <summary>
        /// 取得單位的預設處理人員
        /// </summary>
        private async Task<int?> GetDefaultAssigneeForDepartmentAsync(int? departmentId)
        {
            if (!departmentId.HasValue)
                return null;

            // 找該單位的第一個使用者
            var departmentUser = await _context.DepartmentUsers
                .Where(du => du.DepartmentId == departmentId)
                .OrderBy(du => du.AssignedAt)
                .FirstOrDefaultAsync();

            if (departmentUser != null)
                return departmentUser.UserId;

            // Fallback: 找第一個管理員
            var admin = await _context.Users
                .Where(u => u.Role == UserRole.Admin)
                .FirstOrDefaultAsync();

            return admin?.Id;
        }

        #endregion
    }
}
