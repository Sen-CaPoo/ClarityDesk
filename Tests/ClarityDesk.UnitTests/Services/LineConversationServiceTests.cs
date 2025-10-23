using ClarityDesk.Data;
using ClarityDesk.Models.DTOs;
using ClarityDesk.Models.Entities;
using ClarityDesk.Models.Enums;
using ClarityDesk.Services;
using ClarityDesk.Services.Interfaces;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;
using Xunit;

namespace ClarityDesk.UnitTests.Services
{
    /// <summary>
    /// LineConversationService 單元測試
    /// </summary>
    public class LineConversationServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly Mock<ILogger<LineConversationService>> _mockLogger;
        private readonly Mock<IIssueReportService> _mockIssueReportService;
        private readonly Mock<IDepartmentService> _mockDepartmentService;
        private readonly LineConversationService _service;

        public LineConversationServiceTests()
        {
            // 使用 In-Memory Database
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _dbContext = new ApplicationDbContext(options);

            _mockLogger = new Mock<ILogger<LineConversationService>>();
            _mockIssueReportService = new Mock<IIssueReportService>();
            _mockDepartmentService = new Mock<IDepartmentService>();

            _service = new LineConversationService(
                _dbContext,
                _mockLogger.Object,
                _mockIssueReportService.Object,
                _mockDepartmentService.Object
            );

            // 初始化測試資料
            SeedTestData();
        }

        private void SeedTestData()
        {
            var user = new User
            {
                Id = 1,
                LineUserId = "U_testuser",
                DisplayName = "測試使用者",
                Email = "test@example.com",
                Role = UserRole.User,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var department = new Department
            {
                Id = 1,
                Name = "資訊部",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _dbContext.Users.Add(user);
            _dbContext.Departments.Add(department);
            _dbContext.SaveChanges();
        }

        [Fact]
        public async Task StartConversationAsync_NewSession_ReturnsSessionId()
        {
            // Arrange
            var lineUserId = "U1234567890abcdef1234567890abcdef";
            var userId = 1;

            // Act
            var sessionId = await _service.StartConversationAsync(lineUserId, userId);

            // Assert
            sessionId.Should().NotBeEmpty();

            var session = await _dbContext.LineConversationSessions
                .FirstOrDefaultAsync(s => s.Id == sessionId);

            session.Should().NotBeNull();
            session!.LineUserId.Should().Be(lineUserId);
            session.UserId.Should().Be(userId);
            session.CurrentStep.Should().Be(ConversationStep.AwaitingTitle);
            session.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
        }

        [Fact]
        public async Task StartConversationAsync_ExistingActiveSession_ThrowsException()
        {
            // Arrange
            var lineUserId = "U1234567890abcdef1234567890abcdef";
            var userId = 1;

            // 建立一個進行中的 Session
            await _service.StartConversationAsync(lineUserId, userId);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.StartConversationAsync(lineUserId, userId));
        }

        [Fact]
        public async Task GetActiveSessionAsync_ExistingSession_ReturnsSessionDto()
        {
            // Arrange
            var lineUserId = "U1234567890abcdef1234567890abcdef";
            var userId = 1;
            var sessionId = await _service.StartConversationAsync(lineUserId, userId);

            // Act
            var result = await _service.GetActiveSessionAsync(lineUserId);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(sessionId);
            result.LineUserId.Should().Be(lineUserId);
            result.UserId.Should().Be(userId);
            result.CurrentStep.Should().Be(ConversationStep.AwaitingTitle);
        }

        [Fact]
        public async Task GetActiveSessionAsync_NoSession_ReturnsNull()
        {
            // Arrange
            var lineUserId = "U1234567890abcdef1234567890abcdef";

            // Act
            var result = await _service.GetActiveSessionAsync(lineUserId);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task ProcessUserInputAsync_ValidTitle_AdvancesToNextStep()
        {
            // Arrange
            var lineUserId = "U1234567890abcdef1234567890abcdef";
            var userId = 1;
            await _service.StartConversationAsync(lineUserId, userId);

            var userInput = "系統無法登入";

            // Act
            var response = await _service.ProcessUserInputAsync(lineUserId, userInput);

            // Assert
            response.Should().NotBeNull();
            response.IsValid.Should().BeTrue();
            response.NextStep.Should().Be(ConversationStep.AwaitingDescription);
            response.Message.Should().Contain("請描述問題的詳細內容");

            var session = await _service.GetActiveSessionAsync(lineUserId);
            session!.CurrentStep.Should().Be(ConversationStep.AwaitingDescription);
            session.SessionData.Should().ContainKey("title");
            session.SessionData["title"].Should().Be(userInput);
        }

        [Fact]
        public async Task ProcessUserInputAsync_EmptyInput_ReturnsInvalid()
        {
            // Arrange
            var lineUserId = "U1234567890abcdef1234567890abcdef";
            var userId = 1;
            await _service.StartConversationAsync(lineUserId, userId);

            // Act
            var response = await _service.ProcessUserInputAsync(lineUserId, "   ");

            // Assert
            response.Should().NotBeNull();
            response.IsValid.Should().BeFalse();
            response.Message.Should().Contain("不能為空");

            var session = await _service.GetActiveSessionAsync(lineUserId);
            session!.CurrentStep.Should().Be(ConversationStep.AwaitingTitle);
        }

        [Fact]
        public void ValidateInput_InvalidPhoneNumber_ReturnsInvalid()
        {
            // Act
            var result = _service.ValidateInput(ConversationStep.AwaitingContactPhone, "1234");

            // Assert
            result.IsValid.Should().BeFalse();
            result.ErrorMessage.Should().Contain("手機號碼");
        }

        [Fact]
        public void ValidateInput_ValidPhoneNumber_ReturnsValid()
        {
            // Act
            var result = _service.ValidateInput(ConversationStep.AwaitingContactPhone, "0912-345678");

            // Assert
            result.IsValid.Should().BeTrue();
            result.ErrorMessage.Should().BeNull();
        }

        [Fact]
        public async Task CompleteConversationAsync_ValidData_ReturnsIssueId()
        {
            // Arrange
            var lineUserId = "U1234567890abcdef1234567890abcdef";
            var userId = 1;
            var sessionId = await _service.StartConversationAsync(lineUserId, userId);

            // 模擬完整的對話流程,設定所有必要資料
            await _service.UpdateSessionDataAsync(sessionId, "title", "系統無法登入");
            await _service.UpdateSessionDataAsync(sessionId, "description", "登入時顯示錯誤訊息");
            await _service.UpdateSessionDataAsync(sessionId, "departmentId", 1);
            await _service.UpdateSessionDataAsync(sessionId, "urgency", "High");
            await _service.UpdateSessionDataAsync(sessionId, "contactName", "王小明");
            await _service.UpdateSessionDataAsync(sessionId, "contactPhone", "0912-345678");
            await _service.AdvanceToNextStepAsync(sessionId, ConversationStep.AwaitingConfirmation);

            // 設定 Mock 回傳值
            _mockIssueReportService
                .Setup(s => s.CreateIssueReportAsync(It.IsAny<CreateIssueReportDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(12345);

            // Act
            var issueId = await _service.CompleteConversationAsync(sessionId);

            // Assert
            issueId.Should().Be(12345);

            // 確認 Session 已被刪除
            var session = await _service.GetActiveSessionAsync(lineUserId);
            session.Should().BeNull();

            // 確認呼叫了 IssueReportService
            _mockIssueReportService.Verify(
                s => s.CreateIssueReportAsync(
                    It.Is<CreateIssueReportDto>(dto =>
                        dto.Title == "系統無法登入"
                    ),
                    It.IsAny<CancellationToken>()
                ),
                Times.Once
            );
        }

        [Fact]
        public async Task CancelConversationAsync_RemovesSession()
        {
            // Arrange
            var lineUserId = "U1234567890abcdef1234567890abcdef";
            var userId = 1;
            await _service.StartConversationAsync(lineUserId, userId);

            // Act
            await _service.CancelConversationAsync(lineUserId);

            // Assert
            var session = await _service.GetActiveSessionAsync(lineUserId);
            session.Should().BeNull();
        }

        [Fact]
        public async Task CleanupExpiredSessionsAsync_RemovesExpiredOnly()
        {
            // Arrange
            var lineUserId1 = "U1111111111111111111111111111111";
            var lineUserId2 = "U2222222222222222222222222222222";
            var userId = 1;

            // 建立兩個 Session
            await _service.StartConversationAsync(lineUserId1, userId);
            await _service.StartConversationAsync(lineUserId2, userId);

            // 手動設定第一個 Session 為過期
            var session1 = await _dbContext.LineConversationSessions
                .FirstAsync(s => s.LineUserId == lineUserId1);
            session1.ExpiresAt = DateTime.UtcNow.AddMinutes(-1);
            await _dbContext.SaveChangesAsync();

            // Act
            var cleanedCount = await _service.CleanupExpiredSessionsAsync();

            // Assert
            cleanedCount.Should().Be(1);

            var activeSession1 = await _service.GetActiveSessionAsync(lineUserId1);
            activeSession1.Should().BeNull();

            var activeSession2 = await _service.GetActiveSessionAsync(lineUserId2);
            activeSession2.Should().NotBeNull();
        }

        [Fact]
        public async Task UpdateSessionDataAsync_UpdatesJsonField()
        {
            // Arrange
            var lineUserId = "U1234567890abcdef1234567890abcdef";
            var userId = 1;
            var sessionId = await _service.StartConversationAsync(lineUserId, userId);

            // Act
            await _service.UpdateSessionDataAsync(sessionId, "title", "測試標題");
            await _service.UpdateSessionDataAsync(sessionId, "departmentId", 1);

            // Assert
            var session = await _service.GetActiveSessionAsync(lineUserId);
            session.Should().NotBeNull();
            session!.SessionData.Should().ContainKey("title");
            session.SessionData["title"].ToString().Should().Be("測試標題");
            session.SessionData.Should().ContainKey("departmentId");
        }

        [Fact]
        public async Task AdvanceToNextStepAsync_UpdatesCurrentStep()
        {
            // Arrange
            var lineUserId = "U1234567890abcdef1234567890abcdef";
            var userId = 1;
            var sessionId = await _service.StartConversationAsync(lineUserId, userId);

            // Act
            await _service.AdvanceToNextStepAsync(sessionId, ConversationStep.AwaitingDescription);

            // Assert
            var session = await _service.GetActiveSessionAsync(lineUserId);
            session.Should().NotBeNull();
            session!.CurrentStep.Should().Be(ConversationStep.AwaitingDescription);
        }

        public void Dispose()
        {
            _dbContext.Database.EnsureDeleted();
            _dbContext.Dispose();
        }
    }
}
