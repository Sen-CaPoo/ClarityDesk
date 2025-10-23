using ClarityDesk.Data;
using ClarityDesk.Models.DTOs;
using ClarityDesk.Models.Entities;
using ClarityDesk.Models.Enums;
using ClarityDesk.Services;
using ClarityDesk.Services.Interfaces;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ClarityDesk.UnitTests.Services;

/// <summary>
/// IssueReportService 單元測試
/// </summary>
public class IssueReportServiceTests
{
    private ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new ApplicationDbContext(options);

        // 添加測試資料
        context.Users.Add(new User
        {
            Id = 1,
            LineUserId = "test_user_1",
            DisplayName = "測試使用者1",
            Email = "test1@example.com",
            Role = UserRole.User,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        context.Departments.AddRange(
            new Department { Id = 1, Name = "客服部", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Department { Id = 2, Name = "技術部", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
        );

        context.SaveChanges();
        return context;
    }

    private IIssueReportService CreateService(ApplicationDbContext context)
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var logger = new Mock<ILogger<IssueReportService>>().Object;
        return new IssueReportService(context, cache, logger);
    }

    /// <summary>
    /// 測試建立回報單 - 有效的 DTO 應該回傳回報單 ID
    /// </summary>
    [Fact]
    public async Task CreateIssueReportAsync_ValidDto_ReturnsIssueId()
    {
        // Arrange
        var context = CreateDbContext();
        var service = CreateService(context);
        
        var createDto = new CreateIssueReportDto
        {
            Title = "測試回報單",
            Content = "這是一個測試內容,內容長度需要超過10個字元",
            RecordDate = DateTime.Today,
            Status = IssueStatus.Pending,
            Priority = PriorityLevel.High,
            ReporterName = "測試回報人",
            CustomerName = "測試顧客",
            CustomerPhone = "0912345678",
            AssignedUserId = 1,
            DepartmentIds = new List<int> { 1, 2 }
        };

        // Act
        var issueId = await service.CreateIssueReportAsync(createDto);

        // Assert
        issueId.Should().BeGreaterThan(0);
        var created = await context.IssueReports.FindAsync(issueId);
        created.Should().NotBeNull();
        created!.Title.Should().Be("測試回報單");
    }

    /// <summary>
    /// 測試更新回報單 - 有效的 DTO 應該回傳 true
    /// </summary>
    [Fact]
    public async Task UpdateIssueReportAsync_ValidDto_ReturnsTrue()
    {
        // Arrange
        var context = CreateDbContext();
        var service = CreateService(context);
        
        var user = new User
        {
            DisplayName = "測試使用者",
            Email = "test@example.com",
            LineUserId = "U1234567890",
            Role = UserRole.User,
            IsActive = true
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();
        
        var issue = new IssueReport
        {
            Title = "原始標題",
            Content = "原始內容,需要超過10個字元",
            RecordDate = DateTime.Today,
            Status = IssueStatus.Pending,
            Priority = PriorityLevel.Low,
            ReporterName = "原始回報人",
            CustomerName = "原始顧客",
            CustomerPhone = "0911111111",
            AssignedUserId = user.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.IssueReports.Add(issue);
        await context.SaveChangesAsync();

        var updateDto = new UpdateIssueReportDto
        {
            Id = issue.Id,
            Title = "更新的回報單標題",
            Content = "更新的內容需要超過10個字元才符合驗證規則",
            RecordDate = DateTime.Today,
            Status = IssueStatus.Completed,
            Priority = PriorityLevel.Medium,
            ReporterName = "更新回報人",
            CustomerName = "更新顧客",
            CustomerPhone = "0987654321",
            AssignedUserId = user.Id,
            DepartmentIds = new List<int> { 1 }
        };

        // Act
        var result = await service.UpdateIssueReportAsync(issue.Id, updateDto, user.Id);

        // Assert
        result.Should().BeTrue();
        var updated = await context.IssueReports.FindAsync(issue.Id);
        updated.Should().NotBeNull();
        updated!.Title.Should().Be("更新的回報單標題");
        updated.Status.Should().Be(IssueStatus.Completed);
    }

    /// <summary>
    /// 測試刪除回報單 - 存在的 ID 應該回傳 true
    /// </summary>
    [Fact]
    public async Task DeleteIssueReportAsync_ExistingId_ReturnsTrue()
    {
        // Arrange
        var context = CreateDbContext();
        var service = CreateService(context);
        
        var issue = new IssueReport
        {
            Title = "要刪除的回報單",
            Content = "這是要刪除的內容,超過10個字元",
            RecordDate = DateTime.Today,
            Status = IssueStatus.Pending,
            Priority = PriorityLevel.Low,
            ReporterName = "回報人",
            CustomerName = "顧客",
            CustomerPhone = "0911111111",
            AssignedUserId = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.IssueReports.Add(issue);
        await context.SaveChangesAsync();

        // Act
        var result = await service.DeleteIssueReportAsync(issue.Id);

        // Assert
        result.Should().BeTrue();
        var deleted = await context.IssueReports.FindAsync(issue.Id);
        deleted.Should().BeNull();
    }

    /// <summary>
    /// 測試取得回報單 - 存在的 ID 應該回傳 DTO
    /// </summary>
    [Fact]
    public async Task GetIssueReportByIdAsync_ExistingId_ReturnsDto()
    {
        // Arrange
        var context = CreateDbContext();
        var service = CreateService(context);
        
        var issue = new IssueReport
        {
            Title = "測試回報單",
            Content = "測試內容,超過10個字元",
            RecordDate = DateTime.Today,
            Status = IssueStatus.Pending,
            Priority = PriorityLevel.High,
            ReporterName = "回報人",
            CustomerName = "顧客",
            CustomerPhone = "0911111111",
            AssignedUserId = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.IssueReports.Add(issue);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetIssueReportByIdAsync(issue.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(issue.Id);
        result.Title.Should().Be("測試回報單");
    }

    /// <summary>
    /// 測試取得回報單列表 - 使用篩選條件應該回傳分頁結果
    /// </summary>
    [Fact]
    public async Task GetIssueReportsAsync_WithFilter_ReturnsPagedResult()
    {
        // Arrange
        var context = CreateDbContext();
        var service = CreateService(context);
        
        // 添加測試資料
        context.IssueReports.AddRange(
            new IssueReport
            {
                Title = "高優先級待處理",
                Content = "內容1,超過10個字元",
                RecordDate = DateTime.Today,
                Status = IssueStatus.Pending,
                Priority = PriorityLevel.High,
                ReporterName = "回報人1",
                CustomerName = "顧客1",
                CustomerPhone = "0911111111",
                AssignedUserId = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new IssueReport
            {
                Title = "低優先級已處理",
                Content = "內容2,超過10個字元",
                RecordDate = DateTime.Today,
                Status = IssueStatus.Completed,
                Priority = PriorityLevel.Low,
                ReporterName = "回報人2",
                CustomerName = "顧客2",
                CustomerPhone = "0922222222",
                AssignedUserId = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        );
        await context.SaveChangesAsync();

        var filter = new IssueFilterDto
        {
            Status = IssueStatus.Pending,
            Priority = PriorityLevel.High,
            CurrentPage = 1,
            PageSize = 20
        };

        // Act
        var result = await service.GetIssueReportsAsync(filter);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().NotBeNull();
        result.CurrentPage.Should().Be(1);
        result.PageSize.Should().Be(20);
        result.Items.Count.Should().Be(1);
        result.Items[0].Title.Should().Be("高優先級待處理");
    }

    /// <summary>
    /// 測試取得回報單統計 - 應該回傳統計資訊
    /// </summary>
    [Fact]
    public async Task GetIssueStatisticsAsync_ReturnsStatistics()
    {
        // Arrange
        var context = CreateDbContext();
        var service = CreateService(context);
        
        // 添加測試資料
        context.IssueReports.AddRange(
            new IssueReport
            {
                Title = "待處理1",
                Content = "內容,超過10個字元",
                RecordDate = DateTime.Today,
                Status = IssueStatus.Pending,
                Priority = PriorityLevel.High,
                ReporterName = "回報人",
                CustomerName = "顧客",
                CustomerPhone = "0911111111",
                AssignedUserId = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new IssueReport
            {
                Title = "已處理1",
                Content = "內容,超過10個字元",
                RecordDate = DateTime.Today,
                Status = IssueStatus.Completed,
                Priority = PriorityLevel.Medium,
                ReporterName = "回報人",
                CustomerName = "顧客",
                CustomerPhone = "0922222222",
                AssignedUserId = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new IssueReport
            {
                Title = "已處理2",
                Content = "內容,超過10個字元",
                RecordDate = DateTime.Today,
                Status = IssueStatus.Completed,
                Priority = PriorityLevel.Low,
                ReporterName = "回報人",
                CustomerName = "顧客",
                CustomerPhone = "0933333333",
                AssignedUserId = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        );
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetIssueStatisticsAsync();

        // Assert
        result.Should().NotBeNull();
        result.TotalIssues.Should().Be(3);
        result.PendingIssues.Should().Be(1);
        result.CompletedIssues.Should().Be(2);
        result.HighPriorityIssues.Should().Be(1);
    }
}
