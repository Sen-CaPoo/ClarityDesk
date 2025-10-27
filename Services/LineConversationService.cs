using ClarityDesk.Data;
using ClarityDesk.Models.DTOs;
using ClarityDesk.Models.Entities;
using ClarityDesk.Models.Enums;
using ClarityDesk.Models.Extensions;
using ClarityDesk.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ClarityDesk.Services
{
    /// <summary>
    /// LINE 對話管理服務實作
    /// </summary>
    public class LineConversationService : ILineConversationService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<LineConversationService> _logger;
        private readonly IIssueReportService _issueReportService;
        private readonly IDepartmentService _departmentService;
        private static readonly TimeSpan SessionTimeout = TimeSpan.FromMinutes(30);

        public LineConversationService(
            ApplicationDbContext dbContext,
            ILogger<LineConversationService> logger,
            IIssueReportService issueReportService,
            IDepartmentService departmentService)
        {
            _dbContext = dbContext;
            _logger = logger;
            _issueReportService = issueReportService;
            _departmentService = departmentService;
        }

        public async Task<Guid> StartConversationAsync(
            string lineUserId,
            int userId,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("開始建立對話 Session: LineUserId={LineUserId}, UserId={UserId}", lineUserId, userId);

            // 檢查是否已有進行中的 Session
            var existingSession = await _dbContext.LineConversationSessions
                .FirstOrDefaultAsync(s => s.LineUserId == lineUserId && s.ExpiresAt > DateTime.UtcNow, cancellationToken);

            if (existingSession != null)
            {
                _logger.LogWarning("使用者已有進行中的對話 Session: LineUserId={LineUserId}", lineUserId);
                throw new InvalidOperationException("您目前已有進行中的回報流程,請先完成或取消後再開始新的回報。");
            }

            var session = new LineConversationSession
            {
                Id = Guid.NewGuid(),
                LineUserId = lineUserId,
                UserId = userId,
                CurrentStep = ConversationStep.AwaitingTitle,
                SessionData = "{}",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.Add(SessionTimeout)
            };

            _dbContext.LineConversationSessions.Add(session);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("對話 Session 已建立: SessionId={SessionId}", session.Id);
            return session.Id;
        }

        public async Task<LineConversationSessionDto?> GetActiveSessionAsync(
            string lineUserId,
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("查詢活躍 Session: LineUserId={LineUserId}", lineUserId);

            var session = await _dbContext.LineConversationSessions
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    s => s.LineUserId == lineUserId && s.ExpiresAt > DateTime.UtcNow,
                    cancellationToken);

            return session?.ToDto();
        }

        public async Task<ConversationResponse> ProcessUserInputAsync(
            string lineUserId,
            string userInput,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("處理使用者輸入: LineUserId={LineUserId}, Input={Input}", lineUserId, userInput);

            var session = await _dbContext.LineConversationSessions
                .FirstOrDefaultAsync(s => s.LineUserId == lineUserId && s.ExpiresAt > DateTime.UtcNow, cancellationToken);

            if (session == null)
            {
                _logger.LogWarning("找不到活躍的 Session: LineUserId={LineUserId}", lineUserId);
                return new ConversationResponse
                {
                    IsValid = false,
                    Message = "您目前沒有進行中的回報流程,請輸入「回報問題」開始。",
                    NextStep = null
                };
            }

            // 驗證輸入
            var validationResult = ValidateInput(session.CurrentStep, userInput);
            if (!validationResult.IsValid)
            {
                return new ConversationResponse
                {
                    IsValid = false,
                    Message = validationResult.ErrorMessage ?? "輸入格式不正確,請重新輸入。",
                    NextStep = session.CurrentStep
                };
            }

            // 處理不同步驟的輸入
            var (nextStep, message, quickReplyOptions) = await ProcessStepInputAsync(session, userInput, cancellationToken);

            // 更新 Session
            session.CurrentStep = nextStep;
            session.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);

            return new ConversationResponse
            {
                IsValid = true,
                Message = message,
                NextStep = nextStep,
                QuickReplyOptions = quickReplyOptions
            };
        }

        private async Task<(ConversationStep nextStep, string message, IEnumerable<QuickReplyOption>? quickReplyOptions)> ProcessStepInputAsync(
            LineConversationSession session,
            string userInput,
            CancellationToken cancellationToken)
        {
            var sessionData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(session.SessionData)
                ?? new Dictionary<string, JsonElement>();

            switch (session.CurrentStep)
            {
                case ConversationStep.AwaitingTitle:
                    sessionData["title"] = JsonSerializer.SerializeToElement(userInput);
                    session.SessionData = JsonSerializer.Serialize(sessionData);
                    return (ConversationStep.AwaitingDescription, "✍️ 請描述問題的詳細內容：", null);

                case ConversationStep.AwaitingDescription:
                    sessionData["description"] = JsonSerializer.SerializeToElement(userInput);
                    session.SessionData = JsonSerializer.Serialize(sessionData);

                    // 取得單位列表
                    var departments = await _departmentService.GetAllDepartmentsAsync(activeOnly: true);
                    var deptOptions = departments.Select(d => new QuickReplyOption { Label = d.Name, Data = d.Name }).ToList();

                    return (ConversationStep.AwaitingDepartment, "🏢 請選擇問題所屬單位：", deptOptions);

                case ConversationStep.AwaitingDepartment:
                    // 儲存單位名稱並查詢對應的 ID
                    var allDepartments = await _departmentService.GetAllDepartmentsAsync(activeOnly: true);
                    var selectedDepartment = allDepartments.FirstOrDefault(d => d.Name == userInput);
                    
                    if (selectedDepartment == null)
                    {
                        throw new InvalidOperationException("無效的單位選擇");
                    }
                    
                    sessionData["departmentId"] = JsonSerializer.SerializeToElement(selectedDepartment.Id);
                    sessionData["departmentName"] = JsonSerializer.SerializeToElement(selectedDepartment.Name);
                    session.SessionData = JsonSerializer.Serialize(sessionData);

                    var urgencyOptions = new List<QuickReplyOption>
                    {
                        new() { Label = "🔴 高", Data = "🔴 高" },
                        new() { Label = "🟡 中", Data = "🟡 中" },
                        new() { Label = "🟢 低", Data = "🟢 低" }
                    };

                    return (ConversationStep.AwaitingUrgency, "⚡ 請選擇緊急程度：", urgencyOptions);

                case ConversationStep.AwaitingUrgency:
                    // 將中文顯示名稱對應到英文 enum 值
                    var urgencyValue = userInput switch
                    {
                        "🔴 高" => "High",
                        "🟡 中" => "Medium",
                        "🟢 低" => "Low",
                        _ => "Medium" // 預設值
                    };
                    sessionData["urgency"] = JsonSerializer.SerializeToElement(urgencyValue);
                    sessionData["urgencyDisplay"] = JsonSerializer.SerializeToElement(userInput);
                    session.SessionData = JsonSerializer.Serialize(sessionData);
                    return (ConversationStep.AwaitingContactName, "👤 請輸入聯絡人姓名：", null);

                case ConversationStep.AwaitingContactName:
                    sessionData["contactName"] = JsonSerializer.SerializeToElement(userInput);
                    session.SessionData = JsonSerializer.Serialize(sessionData);
                    return (ConversationStep.AwaitingContactPhone, "📞 請輸入連絡電話：", null);

                case ConversationStep.AwaitingContactPhone:
                    sessionData["contactPhone"] = JsonSerializer.SerializeToElement(userInput);
                    session.SessionData = JsonSerializer.Serialize(sessionData);

                    // 產生摘要訊息
                    var summary = BuildSummaryMessage(sessionData);
                    var confirmOptions = new List<QuickReplyOption>
                    {
                        new() { Label = "✅ 確認送出", Data = "✅ 確認送出" },
                        new() { Label = "❌ 取消", Data = "❌ 取消" }
                    };

                    return (ConversationStep.AwaitingConfirmation, summary, confirmOptions);

                default:
                    throw new InvalidOperationException($"未知的對話步驟: {session.CurrentStep}");
            }
        }

        private string BuildSummaryMessage(Dictionary<string, JsonElement> sessionData)
        {
            var title = sessionData["title"].GetString();
            var description = sessionData["description"].GetString();
            var departmentName = sessionData["departmentName"].GetString();
            var urgencyDisplay = sessionData["urgencyDisplay"].GetString();
            var contactName = sessionData["contactName"].GetString();
            var contactPhone = sessionData["contactPhone"].GetString();

            return $@"📋 回報單摘要
────────────────────
問題標題：{title}
詳細內容：{description}
所屬單位：{departmentName}
緊急程度：{urgencyDisplay}
聯絡人：{contactName}
連絡電話：{contactPhone}
────────────────────
請確認以上資訊是否正確？";
        }

        public async Task UpdateSessionDataAsync(
            Guid sessionId,
            string fieldName,
            object value,
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("更新 Session 資料: SessionId={SessionId}, Field={Field}", sessionId, fieldName);

            var session = await _dbContext.LineConversationSessions
                .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);

            if (session == null)
            {
                _logger.LogWarning("找不到 Session: SessionId={SessionId}", sessionId);
                return;
            }

            var sessionData = JsonSerializer.Deserialize<Dictionary<string, object>>(session.SessionData)
                ?? new Dictionary<string, object>();

            sessionData[fieldName] = value;
            session.SessionData = JsonSerializer.Serialize(sessionData);
            session.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task AdvanceToNextStepAsync(
            Guid sessionId,
            ConversationStep nextStep,
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("推進對話步驟: SessionId={SessionId}, NextStep={NextStep}", sessionId, nextStep);

            var session = await _dbContext.LineConversationSessions
                .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);

            if (session == null)
            {
                _logger.LogWarning("找不到 Session: SessionId={SessionId}", sessionId);
                return;
            }

            session.CurrentStep = nextStep;
            session.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task<int> CompleteConversationAsync(
            Guid sessionId,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("完成對話流程: SessionId={SessionId}", sessionId);

            var session = await _dbContext.LineConversationSessions
                .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);

            if (session == null)
            {
                throw new InvalidOperationException("找不到對話 Session");
            }

            var sessionData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(session.SessionData)
                ?? throw new InvalidOperationException("Session 資料格式錯誤");

            var departmentId = sessionData["departmentId"].GetInt32();

            // 查詢單位的預設處理人員 (取第一個活躍使用者)
            var defaultAssignedUserId = await _dbContext.DepartmentUsers
                .Where(du => du.DepartmentId == departmentId)
                .Join(_dbContext.Users,
                    du => du.UserId,
                    u => u.Id,
                    (du, u) => new { du.UserId, u.IsActive })
                .Where(x => x.IsActive)
                .Select(x => x.UserId)
                .FirstOrDefaultAsync(cancellationToken);

            // 如果沒有預設處理人員，就使用建立回報單的使用者
            var assignedUserId = defaultAssignedUserId > 0 ? defaultAssignedUserId : session.UserId;

            // 建立回報單 DTO
            var createDto = new CreateIssueReportDto
            {
                Title = sessionData["title"].GetString() ?? string.Empty,
                Content = sessionData["description"].GetString() ?? string.Empty,
                RecordDate = DateTime.UtcNow,
                Status = IssueStatus.Pending,
                Priority = Enum.Parse<PriorityLevel>(sessionData["urgency"].GetString() ?? "Medium"),
                ReporterName = sessionData["contactName"].GetString() ?? string.Empty,
                CustomerName = sessionData["contactName"].GetString() ?? string.Empty,
                CustomerPhone = sessionData["contactPhone"].GetString() ?? string.Empty,
                AssignedUserId = assignedUserId,
                DepartmentIds = new List<int> { departmentId }
            };

            // 建立回報單
            var issueId = await _issueReportService.CreateIssueReportAsync(createDto, cancellationToken);

            // 刪除 Session
            _dbContext.LineConversationSessions.Remove(session);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("回報單已建立並完成對話: IssueId={IssueId}, SessionId={SessionId}", issueId, sessionId);
            return issueId;
        }

        public async Task CancelConversationAsync(
            string lineUserId,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("取消對話流程: LineUserId={LineUserId}", lineUserId);

            var session = await _dbContext.LineConversationSessions
                .FirstOrDefaultAsync(s => s.LineUserId == lineUserId && s.ExpiresAt > DateTime.UtcNow, cancellationToken);

            if (session != null)
            {
                _dbContext.LineConversationSessions.Remove(session);
                await _dbContext.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("對話已取消: SessionId={SessionId}", session.Id);
            }
        }

        public ValidationResult ValidateInput(ConversationStep step, string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return new ValidationResult(false, "輸入不能為空,請重新輸入。");
            }

            switch (step)
            {
                case ConversationStep.AwaitingTitle:
                    if (input.Length < 1 || input.Length > 100)
                    {
                        return new ValidationResult(false, "問題標題長度必須介於 1-100 個字元之間。");
                    }
                    break;

                case ConversationStep.AwaitingDescription:
                    if (input.Length < 1 || input.Length > 1000)
                    {
                        return new ValidationResult(false, "問題描述長度必須介於 1-1000 個字元之間。");
                    }
                    break;

                case ConversationStep.AwaitingContactName:
                    if (input.Length < 2 || input.Length > 50)
                    {
                        return new ValidationResult(false, "聯絡人姓名長度必須介於 2-50 個字元之間。");
                    }
                    break;

                case ConversationStep.AwaitingContactPhone:
                    if (input.Length < 1 || input.Length > 50)
                    {
                        return new ValidationResult(false, "電話號碼長度必須介於 1-50 個字元之間。");
                    }
                    break;
            }

            return new ValidationResult(true);
        }

        public async Task<int> CleanupExpiredSessionsAsync(
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("開始清理過期 Session");

            var expiredSessions = await _dbContext.LineConversationSessions
                .Where(s => s.ExpiresAt < DateTime.UtcNow)
                .ToListAsync(cancellationToken);

            if (expiredSessions.Any())
            {
                _dbContext.LineConversationSessions.RemoveRange(expiredSessions);
                await _dbContext.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("已清理 {Count} 個過期 Session", expiredSessions.Count);
            }

            return expiredSessions.Count;
        }
    }
}
