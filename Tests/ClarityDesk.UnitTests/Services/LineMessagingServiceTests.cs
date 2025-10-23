using ClarityDesk.Data;
using ClarityDesk.Models.DTOs;
using ClarityDesk.Models.Entities;
using ClarityDesk.Models.Enums;
using ClarityDesk.Services;
using ClarityDesk.Services.Interfaces;
using FluentAssertions;
using Line.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ClarityDesk.UnitTests.Services
{
    public class LineMessagingServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<ILineMessagingClient> _mockLineClient;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<ILogger<LineMessagingService>> _mockLogger;
        private readonly Mock<IIssueReportTokenService> _mockTokenService;
        private readonly LineMessagingService _service;

        public LineMessagingServiceTests()
        {
            // 建立 In-Memory Database
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new ApplicationDbContext(options);

            // 初始化 Mocks
            _mockLineClient = new Mock<ILineMessagingClient>();
            _mockConfiguration = new Mock<IConfiguration>();
            _mockLogger = new Mock<ILogger<LineMessagingService>>();
            _mockTokenService = new Mock<IIssueReportTokenService>();

            // 設定配置
            _mockConfiguration.Setup(c => c["LineSettings:ChannelAccessToken"]).Returns("test-token");
            _mockConfiguration.Setup(c => c["LineSettings:MonthlyPushLimit"]).Returns("500");
            _mockConfiguration.Setup(c => c["LineSettings:BaseUrl"]).Returns("https://localhost:5001");

            _service = new LineMessagingService(
                _context,
                _mockLineClient.Object,
                _mockConfiguration.Object,
                _mockLogger.Object,
                _mockTokenService.Object);
        }

        [Fact]
        public async Task SendIssueNotificationAsync_ValidData_ReturnsTrue()
        {
            // Arrange
            var lineUserId = "U1234567890abcdef";
            var issueReport = new IssueReportDto
            {
                Id = 1,
                Title = "測試回報單",
                Content = "這是測試描述",
                Status = IssueStatus.Pending,
                Priority = PriorityLevel.High,
                CustomerName = "王小明",
                CustomerPhone = "0912-345678",
                DepartmentNames = new List<string> { "資訊部" },
                CreatedAt = DateTime.UtcNow
            };

            _mockTokenService
                .Setup(t => t.GenerateToken(issueReport.Id))
                .Returns("encrypted-token");

            _mockLineClient
                .Setup(c => c.PushMessageAsync(
                    It.IsAny<string>(),
                    It.IsAny<List<ISendMessage>>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.SendIssueNotificationAsync(lineUserId, issueReport);

            // Assert
            result.Should().BeTrue();
            
            // 驗證日誌已記錄
            var logEntry = await _context.LineMessageLogs
                .FirstOrDefaultAsync(l => l.LineUserId == lineUserId && l.IssueReportId == issueReport.Id);
            logEntry.Should().NotBeNull();
            logEntry!.MessageType.Should().Be(LineMessageType.Push);
            logEntry.Direction.Should().Be(MessageDirection.Outbound);
            logEntry.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public async Task SendIssueNotificationAsync_LineApiError_ReturnsFalse()
        {
            // Arrange
            var lineUserId = "U1234567890abcdef";
            var issueReport = new IssueReportDto
            {
                Id = 2,
                Title = "測試回報單",
                Content = "這是測試描述",
                Status = IssueStatus.Pending,
                Priority = PriorityLevel.Medium,
                CustomerName = "李小華",
                CustomerPhone = "0923-456789",
                DepartmentNames = new List<string> { "總務部" },
                CreatedAt = DateTime.UtcNow
            };

            _mockTokenService
                .Setup(t => t.GenerateToken(issueReport.Id))
                .Returns("encrypted-token");

            _mockLineClient
                .Setup(c => c.PushMessageAsync(
                    It.IsAny<string>(),
                    It.IsAny<List<ISendMessage>>()))
                .ThrowsAsync(new Exception("LINE API Error"));

            // Act
            var result = await _service.SendIssueNotificationAsync(lineUserId, issueReport);

            // Assert
            result.Should().BeFalse();
            
            // 驗證失敗日誌已記錄
            var logEntry = await _context.LineMessageLogs
                .FirstOrDefaultAsync(l => l.LineUserId == lineUserId && l.IssueReportId == issueReport.Id);
            logEntry.Should().NotBeNull();
            logEntry!.IsSuccess.Should().BeFalse();
            logEntry.ErrorMessage.Should().Contain("LINE API Error");
        }

        [Fact]
        public void BuildIssueNotificationFlexMessage_ReturnsValidJson()
        {
            // Arrange
            var issueReport = new IssueReportDto
            {
                Id = 3,
                Title = "電腦無法開機",
                Content = "辦公室電腦突然無法開機，請協助處理",
                Status = IssueStatus.Pending,
                Priority = PriorityLevel.High,
                CustomerName = "陳大明",
                CustomerPhone = "0934-567890",
                DepartmentNames = new List<string> { "資訊部", "總務部" },
                CreatedAt = DateTime.UtcNow
            };

            _mockTokenService
                .Setup(t => t.GenerateToken(issueReport.Id))
                .Returns("test-token-123");

            // Act
            var flexMessageJson = _service.BuildIssueNotificationFlexMessage(issueReport);

            // Assert
            flexMessageJson.Should().NotBeNullOrEmpty();
            flexMessageJson.Should().Contain("電腦無法開機");
            flexMessageJson.Should().Contain("陳大明");
            flexMessageJson.Should().Contain("0934-567890");
            flexMessageJson.Should().Contain("資訊部");
            flexMessageJson.Should().Contain("緊急");
            flexMessageJson.Should().Contain("test-token-123");
        }

        [Fact]
        public async Task CanSendPushMessageAsync_BelowLimit_ReturnsTrue()
        {
            // Arrange
            var currentMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
            
            // 已發送 100 則
            for (int i = 0; i < 100; i++)
            {
                _context.LineMessageLogs.Add(new LineMessageLog
                {
                    LineUserId = $"U{i:D15}",
                    MessageType = LineMessageType.Push,
                    Direction = MessageDirection.Outbound,
                    Content = "{}",
                    IsSuccess = true,
                    SentAt = currentMonth.AddDays(i % 28)
                });
            }
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.CanSendPushMessageAsync();

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task CanSendPushMessageAsync_ExceedLimit_ReturnsFalse()
        {
            // Arrange
            var currentMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
            
            // 已發送 500 則 (達到限制)
            for (int i = 0; i < 500; i++)
            {
                _context.LineMessageLogs.Add(new LineMessageLog
                {
                    LineUserId = $"U{i:D15}",
                    MessageType = LineMessageType.Push,
                    Direction = MessageDirection.Outbound,
                    Content = "{}",
                    IsSuccess = true,
                    SentAt = currentMonth.AddDays(i % 28)
                });
            }
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.CanSendPushMessageAsync();

            // Assert
            result.Should().BeFalse();
        }

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}
