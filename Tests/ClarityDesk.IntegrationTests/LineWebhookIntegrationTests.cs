using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ClarityDesk.Data;
using ClarityDesk.Models.Entities;
using ClarityDesk.Models.Enums;
using Xunit;

namespace ClarityDesk.IntegrationTests;

/// <summary>
/// LINE Webhook 整合測試
/// 測試完整的 Webhook 請求流程,包含簽章驗證、事件處理、資料庫操作
/// </summary>
public class LineWebhookIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly string _channelSecret = "test-channel-secret-12345";

    public LineWebhookIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            // 設定測試用的 Configuration (用於 Middleware 簽章驗證)
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "LineSettings:ChannelSecret", _channelSecret },
                    { "LineSettings:ChannelId", "test-channel-id" },
                    { "LineSettings:ChannelAccessToken", "test-access-token" }
                });
            });

            builder.ConfigureServices(services =>
            {
                // 移除現有的 DbContext 註冊
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                // 使用 In-Memory Database
                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseInMemoryDatabase("LineWebhookIntegrationTestDb");
                });

                // 建立測試資料
                var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                context.Database.EnsureCreated();
                SeedTestData(context);
            });
        });
    }

    private void SeedTestData(ApplicationDbContext context)
    {
        // 清除現有資料
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();

        // 建立測試使用者
        var user = new User
        {
            LineUserId = "U-test-user-001",
            DisplayName = "測試使用者",
            Email = "test@example.com",
            Role = UserRole.User,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Users.Add(user);

        // 建立測試單位
        var department = new Department
        {
            Name = "資訊部",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Departments.Add(department);

        context.SaveChanges();
    }

    /// <summary>
    /// 計算 LINE Webhook 簽章
    /// </summary>
    private string ComputeSignature(string body)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_channelSecret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(body));
        return Convert.ToBase64String(hash);
    }

    [Fact]
    public async Task HandleWebhook_ValidFollowEvent_CreatesLineBinding()
    {
        // Arrange
        var client = _factory.CreateClient();
        
        var webhookPayload = new
        {
            destination = "test-destination",
            events = new[]
            {
                new
                {
                    type = "follow",
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    source = new
                    {
                        type = "user",
                        userId = "U1234567890abcdef"
                    },
                    replyToken = "reply-token-123",
                    mode = "active"
                }
            }
        };

        var jsonBody = JsonSerializer.Serialize(webhookPayload);
        var signature = ComputeSignature(jsonBody);

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/line/webhook")
        {
            Content = new StringContent(jsonBody, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("X-Line-Signature", signature);

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // 驗證資料庫中建立了 LineBinding
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var binding = await context.LineBindings
            .FirstOrDefaultAsync(b => b.LineUserId == "U1234567890abcdef");

        binding.Should().NotBeNull();
        binding!.BindingStatus.Should().Be(BindingStatus.Active);
    }

    [Fact]
    public async Task HandleWebhook_ValidUnfollowEvent_UpdatesBindingStatusToBlocked()
    {
        // Arrange
        using (var scope1 = _factory.Services.CreateScope())
        {
            var dbContext = scope1.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var user = await dbContext.Users.FirstAsync();
            
            // 先建立一個綁定
            var testBinding = new LineBinding
            {
                UserId = user.Id,
                LineUserId = "U1234567890abcdef",
                DisplayName = "測試使用者",
                BindingStatus = BindingStatus.Active,
                BoundAt = DateTime.UtcNow,
                LastInteractedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            dbContext.LineBindings.Add(testBinding);
            await dbContext.SaveChangesAsync();
        }

        var client = _factory.CreateClient();
        
        var webhookPayload = new
        {
            destination = "test-destination",
            events = new[]
            {
                new
                {
                    type = "unfollow",
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    source = new
                    {
                        type = "user",
                        userId = "U1234567890abcdef"
                    },
                    mode = "active"
                }
            }
        };

        var jsonBody = JsonSerializer.Serialize(webhookPayload);
        var signature = ComputeSignature(jsonBody);

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/line/webhook")
        {
            Content = new StringContent(jsonBody, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("X-Line-Signature", signature);

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // 驗證資料庫中更新了 LineBinding 狀態
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var binding = await context.LineBindings
            .FirstOrDefaultAsync(b => b.LineUserId == "U1234567890abcdef");

        binding.Should().NotBeNull();
        binding!.BindingStatus.Should().Be(BindingStatus.Blocked);
    }

    [Fact]
    public async Task HandleWebhook_MessageEventWithTriggerKeyword_StartsConversation()
    {
        // Arrange
        using (var scope2 = _factory.Services.CreateScope())
        {
            var dbContext = scope2.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var user = await dbContext.Users.FirstAsync();
            
            // 建立 LINE 綁定
            var testBinding = new LineBinding
            {
                UserId = user.Id,
                LineUserId = "U1234567890abcdef",
                DisplayName = "測試使用者",
                BindingStatus = BindingStatus.Active,
                BoundAt = DateTime.UtcNow,
                LastInteractedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            dbContext.LineBindings.Add(testBinding);
            await dbContext.SaveChangesAsync();
        }

        var client = _factory.CreateClient();
        
        var webhookPayload = new
        {
            destination = "test-destination",
            events = new[]
            {
                new
                {
                    type = "message",
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    source = new
                    {
                        type = "user",
                        userId = "U1234567890abcdef"
                    },
                    replyToken = "reply-token-123",
                    message = new
                    {
                        type = "text",
                        id = "msg-123",
                        text = "回報問題"
                    },
                    mode = "active"
                }
            }
        };

        var jsonBody = JsonSerializer.Serialize(webhookPayload);
        var signature = ComputeSignature(jsonBody);

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/line/webhook")
        {
            Content = new StringContent(jsonBody, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("X-Line-Signature", signature);

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // 驗證資料庫中建立了對話 Session
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var session = await context.LineConversationSessions
            .FirstOrDefaultAsync(s => s.LineUserId == "U1234567890abcdef");

        session.Should().NotBeNull();
        session!.CurrentStep.Should().Be(ConversationStep.AwaitingTitle);
        session.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task HandleWebhook_InvalidSignature_ReturnsUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();
        
        var webhookPayload = new
        {
            destination = "test-destination",
            events = new[]
            {
                new
                {
                    type = "follow",
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    source = new
                    {
                        type = "user",
                        userId = "U1234567890abcdef"
                    },
                    replyToken = "reply-token-123",
                    mode = "active"
                }
            }
        };

        var jsonBody = JsonSerializer.Serialize(webhookPayload);
        var invalidSignature = "invalid-signature-12345";

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/line/webhook")
        {
            Content = new StringContent(jsonBody, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("X-Line-Signature", invalidSignature);

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task HandleWebhook_MissingSignature_ReturnsUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();
        
        var webhookPayload = new
        {
            destination = "test-destination",
            events = new[]
            {
                new
                {
                    type = "follow",
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    source = new
                    {
                        type = "user",
                        userId = "U1234567890abcdef"
                    },
                    replyToken = "reply-token-123",
                    mode = "active"
                }
            }
        };

        var jsonBody = JsonSerializer.Serialize(webhookPayload);

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/line/webhook")
        {
            Content = new StringContent(jsonBody, Encoding.UTF8, "application/json")
        };
        // 不設定 X-Line-Signature header

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task HandleWebhook_CompleteConversationFlow_CreatesIssueReport()
    {
        // Arrange
        using (var scope3 = _factory.Services.CreateScope())
        {
            var dbContext = scope3.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var user = await dbContext.Users.FirstAsync();
            var department = await dbContext.Departments.FirstAsync();
            
            // 建立 LINE 綁定
            var testBinding = new LineBinding
            {
                UserId = user.Id,
                LineUserId = "U1234567890abcdef",
                DisplayName = "測試使用者",
                BindingStatus = BindingStatus.Active,
                BoundAt = DateTime.UtcNow,
                LastInteractedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            dbContext.LineBindings.Add(testBinding);

            // 建立進行中的對話 Session (模擬已填寫到確認步驟)
            var sessionData = new Dictionary<string, object>
            {
                { "Title", "電腦無法開機" },
                { "Description", "按下電源鍵後沒有反應" },
                { "DepartmentId", department.Id },
                { "Priority", (int)PriorityLevel.High },
                { "CustomerName", "測試使用者" },
                { "CustomerPhone", "0912-345678" }
            };

            var testSession = new LineConversationSession
            {
                UserId = user.Id,
                LineUserId = "U1234567890abcdef",
                CurrentStep = ConversationStep.AwaitingConfirmation,
                SessionData = JsonSerializer.Serialize(sessionData),
                ExpiresAt = DateTime.UtcNow.AddMinutes(30),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            dbContext.LineConversationSessions.Add(testSession);
            await dbContext.SaveChangesAsync();
        }

        var client = _factory.CreateClient();
        
        // 模擬使用者確認提交的 Postback 事件
        var webhookPayload = new
        {
            destination = "test-destination",
            events = new[]
            {
                new
                {
                    type = "postback",
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    source = new
                    {
                        type = "user",
                        userId = "U1234567890abcdef"
                    },
                    replyToken = "reply-token-123",
                    postback = new
                    {
                        data = "action=confirm_submit"
                    },
                    mode = "active"
                }
            }
        };

        var jsonBody = JsonSerializer.Serialize(webhookPayload);
        var signature = ComputeSignature(jsonBody);

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/line/webhook")
        {
            Content = new StringContent(jsonBody, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("X-Line-Signature", signature);

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // 驗證資料庫中建立了回報單
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var issueReport = await context.IssueReports
            .Include(i => i.DepartmentAssignments)
            .FirstOrDefaultAsync(i => i.Title == "電腦無法開機");

        issueReport.Should().NotBeNull();
        issueReport!.Content.Should().Be("按下電源鍵後沒有反應");
        issueReport.Priority.Should().Be(PriorityLevel.High);
        issueReport.CustomerName.Should().Be("測試使用者");
        issueReport.CustomerPhone.Should().Be("0912-345678");
        issueReport.DepartmentAssignments.Should().HaveCount(1);

        // 驗證 Session 已被清除 (檢查是否還有未過期的 session)
        var activeSession = await context.LineConversationSessions
            .FirstOrDefaultAsync(s => s.LineUserId == "U1234567890abcdef" && s.ExpiresAt > DateTime.UtcNow);
        activeSession.Should().BeNull();
    }
}
