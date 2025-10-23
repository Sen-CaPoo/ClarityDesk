using ClarityDesk.Data;
using ClarityDesk.Models.Entities;
using ClarityDesk.Models.Enums;
using ClarityDesk.Services;
using ClarityDesk.Services.Interfaces;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Cryptography;
using System.Text;
using Xunit;

namespace ClarityDesk.UnitTests.Services
{
    /// <summary>
    /// LineWebhookHandler 單元測試
    /// </summary>
    public class LineWebhookHandlerTests : IDisposable
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly Mock<ILogger<LineWebhookHandler>> _mockLogger;
        private readonly Mock<ILineBindingService> _mockLineBindingService;
        private readonly Mock<ILineMessagingService> _mockLineMessagingService;
        private readonly Mock<ILineConversationService> _mockLineConversationService;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly LineWebhookHandler _handler;
        private const string TestChannelSecret = "test_channel_secret_12345678";

        public LineWebhookHandlerTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _dbContext = new ApplicationDbContext(options);

            _mockLogger = new Mock<ILogger<LineWebhookHandler>>();
            _mockLineBindingService = new Mock<ILineBindingService>();
            _mockLineMessagingService = new Mock<ILineMessagingService>();
            _mockLineConversationService = new Mock<ILineConversationService>();
            _mockConfiguration = new Mock<IConfiguration>();

            // 設定 Configuration Mock
            _mockConfiguration.Setup(c => c["LineSettings:ChannelSecret"]).Returns(TestChannelSecret);

            _handler = new LineWebhookHandler(
                _mockLogger.Object,
                _mockLineBindingService.Object,
                _mockLineMessagingService.Object,
                _mockLineConversationService.Object,
                _mockConfiguration.Object
            );

            SeedTestData();
        }

        private void SeedTestData()
        {
            var user = new User
            {
                Id = 1,
                LineUserId = "U1234567890abcdef1234567890abcdef",
                DisplayName = "測試使用者",
                Email = "test@example.com",
                Role = UserRole.User,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _dbContext.Users.Add(user);
            _dbContext.SaveChanges();
        }

        [Fact]
        public void ValidateSignature_ValidSignature_ReturnsTrue()
        {
            // Arrange
            var payload = "{\"events\":[]}";
            var signature = ComputeHmacSha256(payload, TestChannelSecret);

            // Act
            var result = _handler.ValidateSignature(payload, signature);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void ValidateSignature_InvalidSignature_ReturnsFalse()
        {
            // Arrange
            var payload = "{\"events\":[]}";
            var invalidSignature = "invalid_signature_123";

            // Act
            var result = _handler.ValidateSignature(payload, invalidSignature);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void ValidateSignature_EmptySignature_ReturnsFalse()
        {
            // Arrange
            var payload = "{\"events\":[]}";
            var emptySignature = string.Empty;

            // Act
            var result = _handler.ValidateSignature(payload, emptySignature);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task HandleFollowEventAsync_NewUser_CreatesBinding()
        {
            // Arrange
            var lineUserId = "U_newuser123456789012345678901234";
            var replyToken = "reply_token_123";

            _mockLineBindingService
                .Setup(s => s.MarkAsActiveAsync(lineUserId, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _mockLineMessagingService
                .Setup(s => s.ReplyMessageAsync(replyToken, It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            await _handler.HandleFollowEventAsync(lineUserId, replyToken);

            // Assert
            _mockLineBindingService.Verify(
                s => s.MarkAsActiveAsync(lineUserId, It.IsAny<CancellationToken>()),
                Times.Once
            );

            _mockLineMessagingService.Verify(
                s => s.ReplyMessageAsync(replyToken, It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()),
                Times.Once
            );
        }

        [Fact]
        public async Task HandleUnfollowEventAsync_UpdatesStatusToBlocked()
        {
            // Arrange
            var lineUserId = "U1234567890abcdef1234567890abcdef";

            _mockLineBindingService
                .Setup(s => s.MarkAsBlockedAsync(lineUserId, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            await _handler.HandleUnfollowEventAsync(lineUserId);

            // Assert
            _mockLineBindingService.Verify(
                s => s.MarkAsBlockedAsync(lineUserId, It.IsAny<CancellationToken>()),
                Times.Once
            );
        }

        [Fact]
        public async Task HandleMessageEventAsync_TriggerKeyword_StartsConversation()
        {
            // Arrange
            var lineUserId = "U1234567890abcdef1234567890abcdef";
            var messageText = "回報問題";
            var replyToken = "reply_token_123";
            var sessionId = Guid.NewGuid();

            var binding = new Models.DTOs.LineBindingDto
            {
                Id = 1,
                UserId = 1,
                LineUserId = lineUserId,
                DisplayName = "測試使用者",
                BindingStatus = BindingStatus.Active,
                BoundAt = DateTime.UtcNow,
                LastInteractedAt = DateTime.UtcNow
            };

            _mockLineConversationService
                .Setup(s => s.GetActiveSessionAsync(lineUserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Models.DTOs.LineConversationSessionDto?)null);

            _mockLineBindingService
                .Setup(s => s.GetBindingByLineUserIdAsync(lineUserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(binding);

            _mockLineBindingService
                .Setup(s => s.UpdateLastInteractionAsync(lineUserId, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _mockLineConversationService
                .Setup(s => s.StartConversationAsync(lineUserId, It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(sessionId);

            _mockLineMessagingService
                .Setup(s => s.ReplyMessageAsync(replyToken, It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            await _handler.HandleMessageEventAsync(lineUserId, messageText, replyToken);

            // Assert
            _mockLineConversationService.Verify(
                s => s.StartConversationAsync(lineUserId, It.IsAny<int>(), It.IsAny<CancellationToken>()),
                Times.Once
            );

            _mockLineMessagingService.Verify(
                s => s.ReplyMessageAsync(replyToken, It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()),
                Times.Once
            );
        }

        [Fact]
        public async Task HandleMessageEventAsync_InConversation_ProcessesInput()
        {
            // Arrange
            var lineUserId = "U1234567890abcdef1234567890abcdef";
            var messageText = "系統無法登入";
            var replyToken = "reply_token_123";

            var activeSession = new Models.DTOs.LineConversationSessionDto
            {
                Id = Guid.NewGuid(),
                LineUserId = lineUserId,
                UserId = 1,
                CurrentStep = ConversationStep.AwaitingTitle,
                SessionData = new Dictionary<string, object>(),
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(30)
            };

            var conversationResponse = new Models.DTOs.ConversationResponse
            {
                IsValid = true,
                Message = "請描述問題的詳細內容:",
                NextStep = ConversationStep.AwaitingDescription,
                QuickReplyOptions = null
            };

            _mockLineBindingService
                .Setup(s => s.UpdateLastInteractionAsync(lineUserId, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _mockLineConversationService
                .Setup(s => s.GetActiveSessionAsync(lineUserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(activeSession);

            _mockLineConversationService
                .Setup(s => s.ProcessUserInputAsync(lineUserId, messageText, It.IsAny<CancellationToken>()))
                .ReturnsAsync(conversationResponse);

            _mockLineMessagingService
                .Setup(s => s.ReplyMessageAsync(replyToken, It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            await _handler.HandleMessageEventAsync(lineUserId, messageText, replyToken);

            // Assert
            _mockLineConversationService.Verify(
                s => s.ProcessUserInputAsync(lineUserId, messageText, It.IsAny<CancellationToken>()),
                Times.Once
            );

            _mockLineMessagingService.Verify(
                s => s.ReplyMessageAsync(replyToken, It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()),
                Times.Once
            );
        }

        [Fact]
        public async Task HandleWebhookAsync_InvalidSignature_Returns401()
        {
            // Arrange
            var payload = "{\"events\":[]}";
            var invalidSignature = "invalid_signature";

            // Act
            var result = await _handler.HandleWebhookAsync(payload, invalidSignature);

            // Assert
            result.Should().Be(401);
        }

        [Fact]
        public async Task HandleWebhookAsync_ValidFollowEvent_Returns200()
        {
            // Arrange
            var payload = @"{
                ""events"": [
                    {
                        ""type"": ""follow"",
                        ""source"": {
                            ""type"": ""user"",
                            ""userId"": ""U1234567890abcdef1234567890abcdef""
                        },
                        ""replyToken"": ""reply_token_123""
                    }
                ]
            }";
            var signature = ComputeHmacSha256(payload, TestChannelSecret);

            _mockLineBindingService
                .Setup(s => s.MarkAsActiveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _mockLineMessagingService
                .Setup(s => s.ReplyMessageAsync(It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            var result = await _handler.HandleWebhookAsync(payload, signature);

            // Assert
            result.Should().Be(200);
        }

        private static string ComputeHmacSha256(string payload, string secret)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            return Convert.ToBase64String(hash);
        }

        public void Dispose()
        {
            _dbContext.Database.EnsureDeleted();
            _dbContext.Dispose();
        }
    }
}
