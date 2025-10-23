using ClarityDesk.Data;
using ClarityDesk.Models.DTOs;
using ClarityDesk.Models.Entities;
using ClarityDesk.Models.Enums;
using ClarityDesk.Services.Interfaces;
using Line.Messaging;
using Line.Messaging.Webhooks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ClarityDesk.Services
{
    /// <summary>
    /// LINE 訊息發送服務實作
    /// </summary>
    public class LineMessagingService : ILineMessagingService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILineMessagingClient _lineClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<LineMessagingService> _logger;
        private readonly IIssueReportTokenService _tokenService;

        public LineMessagingService(
            ApplicationDbContext context,
            ILineMessagingClient lineClient,
            IConfiguration configuration,
            ILogger<LineMessagingService> logger,
            IIssueReportTokenService tokenService)
        {
            _context = context;
            _lineClient = lineClient;
            _configuration = configuration;
            _logger = logger;
            _tokenService = tokenService;
        }

        public async Task<bool> SendIssueNotificationAsync(
            string lineUserId,
            IssueReportDto issueReport,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("準備發送回報單通知: IssueId={IssueId}, LineUserId={LineUserId}",
                issueReport.Id, lineUserId);

            try
            {
                // 檢查配額
                if (!await CanSendPushMessageAsync(cancellationToken))
                {
                    _logger.LogWarning("已達每月推送配額限制,無法發送通知");
                    await LogFailedMessageAsync(lineUserId, issueReport.Id, "QUOTA_EXCEEDED", "已達每月配額限制");
                    return false;
                }

                // 建構 Flex Message  
                var flexMessageJson = BuildIssueNotificationFlexMessage(issueReport);
                
                // 暫時使用文字訊息代替 Flex Message (簡化實作)
                var summaryText = $"📋 新回報單通知\\n\\n" +
                    $"單號: #{issueReport.Id:D6}\\n" +
                    $"標題: {issueReport.Title}\\n" +
                    $"緊急程度: {issueReport.PriorityText}\\n" +
                    $"單位: {string.Join(", ", issueReport.DepartmentNames)}\\n" +
                    $"聯絡人: {issueReport.CustomerName}\\n" +
                    $"電話: {issueReport.CustomerPhone}\\n\\n" +
                    $"請盡快處理!";
                
                var messages = new List<ISendMessage> { new TextMessage(summaryText) };

                // 發送訊息
                await _lineClient.PushMessageAsync(lineUserId, messages);

                // 記錄成功日誌
                await LogSuccessfulMessageAsync(lineUserId, issueReport.Id, flexMessageJson);

                _logger.LogInformation("回報單通知發送成功: IssueId={IssueId}", issueReport.Id);
                return true;
            }
            catch (LineResponseException ex)
            {
                _logger.LogError(ex, "LINE API 回應錯誤: IssueId={IssueId}, StatusCode={StatusCode}",
                    issueReport.Id, ex.StatusCode);

                // 記錄失敗日誌
                await LogFailedMessageAsync(lineUserId, issueReport.Id, 
                    ex.StatusCode.ToString(), ex.Message);

                // 特定錯誤碼處理
                if (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    _logger.LogWarning("使用者已封鎖官方帳號: LineUserId={LineUserId}", lineUserId);
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "發送通知時發生未預期錯誤: IssueId={IssueId}", issueReport.Id);
                await LogFailedMessageAsync(lineUserId, issueReport.Id, "UNEXPECTED_ERROR", ex.Message);
                return false;
            }
        }

        public async Task<bool> ReplyMessageAsync(
            string replyToken,
            IEnumerable<string> messages,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var sendMessages = messages.Select(m => (ISendMessage)new TextMessage(m)).ToList();
                await _lineClient.ReplyMessageAsync(replyToken, sendMessages);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "回覆訊息失敗: ReplyToken={ReplyToken}", replyToken);
                return false;
            }
        }

        public async Task<bool> PushTextMessageAsync(
            string lineUserId,
            string message,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (!await CanSendPushMessageAsync(cancellationToken))
                {
                    _logger.LogWarning("已達每月推送配額限制");
                    return false;
                }

                var messages = new List<ISendMessage> { new TextMessage(message) };
                await _lineClient.PushMessageAsync(lineUserId, messages);

                await LogSuccessfulMessageAsync(lineUserId, null, message);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "推送文字訊息失敗: LineUserId={LineUserId}", lineUserId);
                await LogFailedMessageAsync(lineUserId, null, "PUSH_ERROR", ex.Message);
                return false;
            }
        }

        public string BuildIssueNotificationFlexMessage(IssueReportDto issueReport)
        {
            var token = _tokenService.GenerateToken(issueReport.Id);
            var baseUrl = _configuration["LineSettings:BaseUrl"] ?? "https://localhost:5001";
            var detailsUrl = $"{baseUrl}/Issues/Details/{issueReport.Id}?token={token}";

            var urgencyColor = issueReport.Priority switch
            {
                PriorityLevel.High => "#FF0000",
                PriorityLevel.Medium => "#FFA500",
                PriorityLevel.Low => "#008000",
                _ => "#808080"
            };

            var urgencyText = issueReport.Priority switch
            {
                PriorityLevel.High => "緊急",
                PriorityLevel.Medium => "一般",
                PriorityLevel.Low => "低",
                _ => "未指定"
            };

            var departments = issueReport.DepartmentNames != null && issueReport.DepartmentNames.Any()
                ? string.Join(", ", issueReport.DepartmentNames)
                : "未指派";

            var flexMessage = new
            {
                type = "bubble",
                hero = new
                {
                    type = "box",
                    layout = "vertical",
                    contents = new[]
                    {
                        new
                        {
                            type = "text",
                            text = "新回報單通知",
                            weight = "bold",
                            size = "xl",
                            color = "#FFFFFF"
                        }
                    },
                    backgroundColor = "#1E90FF",
                    paddingAll = "20px"
                },
                body = new
                {
                    type = "box",
                    layout = "vertical",
                    contents = new object[]
                    {
                        new
                        {
                            type = "text",
                            text = issueReport.Title,
                            weight = "bold",
                            size = "lg",
                            wrap = true,
                            color = "#333333"
                        },
                        new
                        {
                            type = "box",
                            layout = "vertical",
                            margin = "lg",
                            spacing = "sm",
                            contents = new object[]
                            {
                                new
                                {
                                    type = "box",
                                    layout = "baseline",
                                    spacing = "sm",
                                    contents = new object[]
                                    {
                                        new { type = "text", text = "單號", color = "#888888", size = "sm", flex = 2 },
                                        new { type = "text", text = $"#{issueReport.Id:D6}", wrap = true, color = "#333333", size = "sm", flex = 5 }
                                    }
                                },
                                new
                                {
                                    type = "box",
                                    layout = "baseline",
                                    spacing = "sm",
                                    contents = new object[]
                                    {
                                        new { type = "text", text = "緊急程度", color = "#888888", size = "sm", flex = 2 },
                                        new { type = "text", text = urgencyText, wrap = true, color = urgencyColor, size = "sm", flex = 5, weight = "bold" }
                                    }
                                },
                                new
                                {
                                    type = "box",
                                    layout = "baseline",
                                    spacing = "sm",
                                    contents = new object[]
                                    {
                                        new { type = "text", text = "負責單位", color = "#888888", size = "sm", flex = 2 },
                                        new { type = "text", text = departments, wrap = true, color = "#333333", size = "sm", flex = 5 }
                                    }
                                },
                                new
                                {
                                    type = "box",
                                    layout = "baseline",
                                    spacing = "sm",
                                    contents = new object[]
                                    {
                                        new { type = "text", text = "聯絡人", color = "#888888", size = "sm", flex = 2 },
                                        new { type = "text", text = issueReport.CustomerName, wrap = true, color = "#333333", size = "sm", flex = 5 }
                                    }
                                },
                                new
                                {
                                    type = "box",
                                    layout = "baseline",
                                    spacing = "sm",
                                    contents = new object[]
                                    {
                                        new { type = "text", text = "電話", color = "#888888", size = "sm", flex = 2 },
                                        new { type = "text", text = issueReport.CustomerPhone, wrap = true, color = "#333333", size = "sm", flex = 5 }
                                    }
                                }
                            }
                        }
                    }
                },
                footer = new
                {
                    type = "box",
                    layout = "vertical",
                    spacing = "sm",
                    contents = new[]
                    {
                        new
                        {
                            type = "button",
                            style = "primary",
                            height = "sm",
                            action = new
                            {
                                type = "uri",
                                label = "查看詳情",
                                uri = detailsUrl
                            }
                        }
                    },
                    flex = 0
                }
            };

            return JsonSerializer.Serialize(flexMessage, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            });
        }

        public string BuildQuickReplyButtons(
            IEnumerable<QuickReplyOption> options,
            int maxOptions = 13)
        {
            var limitedOptions = options.Take(maxOptions);
            var quickReplyItems = limitedOptions.Select(opt => new
            {
                type = "action",
                action = new
                {
                    type = "message",
                    label = opt.Label,
                    text = opt.Data
                }
            }).ToArray();

            var quickReply = new
            {
                items = quickReplyItems
            };

            return JsonSerializer.Serialize(quickReply, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }

        public async Task<bool> CanSendPushMessageAsync(CancellationToken cancellationToken = default)
        {
            var monthlyLimit = int.Parse(_configuration["LineSettings:MonthlyPushLimit"] ?? "500");
            var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

            var pushCount = await _context.LineMessageLogs
                .Where(l => l.MessageType == LineMessageType.Push
                    && l.Direction == MessageDirection.Outbound
                    && l.IsSuccess
                    && l.SentAt >= startOfMonth)
                .CountAsync(cancellationToken);

            return pushCount < monthlyLimit;
        }

        public async Task LogMessageAsync(
            LineMessageLogDto log,
            CancellationToken cancellationToken = default)
        {
            var entity = new LineMessageLog
            {
                LineUserId = log.LineUserId,
                MessageType = log.MessageType,
                Direction = log.Direction,
                Content = log.Content,
                IsSuccess = log.IsSuccess,
                ErrorCode = log.ErrorCode,
                ErrorMessage = log.ErrorMessage,
                IssueReportId = log.IssueReportId,
                SentAt = DateTime.UtcNow
            };

            _context.LineMessageLogs.Add(entity);
            await _context.SaveChangesAsync(cancellationToken);
        }

        private async Task LogSuccessfulMessageAsync(string lineUserId, int? issueReportId, string content)
        {
            await LogMessageAsync(new LineMessageLogDto
            {
                LineUserId = lineUserId,
                MessageType = LineMessageType.Push,
                Direction = MessageDirection.Outbound,
                Content = content,
                IsSuccess = true,
                IssueReportId = issueReportId
            });
        }

        private async Task LogFailedMessageAsync(string lineUserId, int? issueReportId, string errorCode, string errorMessage)
        {
            await LogMessageAsync(new LineMessageLogDto
            {
                LineUserId = lineUserId,
                MessageType = LineMessageType.Push,
                Direction = MessageDirection.Outbound,
                Content = "{}",
                IsSuccess = false,
                ErrorCode = errorCode,
                ErrorMessage = errorMessage,
                IssueReportId = issueReportId
            });
        }
    }
}
