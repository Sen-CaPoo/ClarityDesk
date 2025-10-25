using ClarityDesk.Data;
using ClarityDesk.Infrastructure.Helpers;
using ClarityDesk.Models.DTOs;
using ClarityDesk.Models.Entities;
using ClarityDesk.Models.Enums;
using ClarityDesk.Models.Extensions;
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
                
                // 將 UTC 時間轉換為台北時間後格式化
                var taipeiTime = TimeZoneHelper.ConvertToTaipeiTime(issueReport.CreatedAt);
                var recordDateStr = taipeiTime.ToString("yyyy/MM/dd HH:mm");
                
                // 產生回報單編號格式 (例如: #20251023-001)
                var issueNumberStr = $"#{taipeiTime:yyyyMMdd}-{issueReport.Id:D3}";
                
                // 緊急程度 emoji
                var priorityEmoji = issueReport.Priority switch
                {
                    PriorityLevel.High => "🔴",
                    PriorityLevel.Medium => "🟡",
                    PriorityLevel.Low => "🟢",
                    _ => "⚪"
                };
                
                // 單位列表
                var departmentsStr = issueReport.DepartmentNames != null && issueReport.DepartmentNames.Any()
                    ? string.Join("、", issueReport.DepartmentNames)
                    : "未指派";
                
                // 產生查看連結 (不需要 token)
                var baseUrl = _configuration["LineSettings:BaseUrl"] ?? "https://localhost:5001";
                var detailsUrl = $"{baseUrl}/Issues/Details/{issueReport.Id}";
                
                // 使用規格定義的文字訊息格式
                var summaryText = 
                    $"【新問題回報】您有一則新的問題待處理\n" +
                    $"━━━━━━━━━━━━━━━━━━━━\n" +
                    $"📋 回報單編號:{issueNumberStr}\n" +
                    $"📌 問題標題:{issueReport.Title}\n" +
                    $"{priorityEmoji} 緊急程度:{issueReport.PriorityText}\n" +
                    $"🏢 問題所屬單位:{departmentsStr}\n" +
                    $"👤 聯絡人:{issueReport.CustomerName}\n" +
                    $"📞 連絡電話:{issueReport.CustomerPhone}\n" +
                    $"📅 紀錄日期:{recordDateStr}\n" +
                    $"✍️ 回報人:{issueReport.ReporterName}\n" +
                    $"━━━━━━━━━━━━━━━━━━━━\n" +
                    $"[查看回報單詳情] 👉 {detailsUrl}";
                
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

        public async Task<bool> ReplyMessageWithQuickReplyAsync(
            string replyToken,
            string message,
            IEnumerable<QuickReplyOption> quickReplyOptions,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // 建構 Quick Reply 項目 (最多 13 個)
                var quickReplyItems = quickReplyOptions
                    .Take(13)
                    .Select(opt => new QuickReplyButtonObject(
                        new MessageTemplateAction(opt.Label, opt.Data)))
                    .ToList();

                var quickReply = new Line.Messaging.QuickReply(quickReplyItems);
                var textMessage = new TextMessage(message, quickReply);

                await _lineClient.ReplyMessageAsync(replyToken, new List<ISendMessage> { textMessage });
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "回覆訊息 (Quick Reply) 失敗: ReplyToken={ReplyToken}", replyToken);
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

        public async Task<PagedResult<LineMessageLogDto>> GetMessageLogsAsync(
            string? lineUserId = null,
            MessageDirection? direction = null,
            bool? isSuccess = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            int pageNumber = 1,
            int pageSize = 50,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("查詢 LINE 訊息日誌: LineUserId={LineUserId}, Direction={Direction}, IsSuccess={IsSuccess}, StartDate={StartDate}, EndDate={EndDate}, Page={Page}",
                    lineUserId, direction, isSuccess, startDate, endDate, pageNumber);

                var query = _context.LineMessageLogs.AsNoTracking();

                // 篩選條件
                if (!string.IsNullOrWhiteSpace(lineUserId))
                {
                    query = query.Where(l => l.LineUserId == lineUserId);
                }

                if (direction.HasValue)
                {
                    query = query.Where(l => l.Direction == direction.Value);
                }

                if (isSuccess.HasValue)
                {
                    query = query.Where(l => l.IsSuccess == isSuccess.Value);
                }

                if (startDate.HasValue)
                {
                    query = query.Where(l => l.SentAt >= startDate.Value);
                }

                if (endDate.HasValue)
                {
                    // 包含整天，所以加上一天
                    var endOfDay = endDate.Value.Date.AddDays(1);
                    query = query.Where(l => l.SentAt < endOfDay);
                }

                var totalCount = await query.CountAsync(cancellationToken);

                var logs = await query
                    .OrderByDescending(l => l.SentAt)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(cancellationToken);

                var dtos = logs.Select(l => l.ToDto()).ToList();

                return PagedResult<LineMessageLogDto>.Create(dtos, totalCount, pageNumber, pageSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查詢 LINE 訊息日誌時發生錯誤");
                throw;
            }
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
