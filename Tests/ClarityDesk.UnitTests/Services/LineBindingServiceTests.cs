using ClarityDesk.Data;
using ClarityDesk.Models.DTOs;
using ClarityDesk.Models.Entities;
using ClarityDesk.Models.Enums;
using ClarityDesk.Services;
using ClarityDesk.Services.Exceptions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ClarityDesk.UnitTests.Services;

/// <summary>
/// LineBindingService 單元測試
/// </summary>
public class LineBindingServiceTests
{
    private ApplicationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private LineBindingService CreateService(ApplicationDbContext context)
    {
        var logger = new Mock<ILogger<LineBindingService>>();
        var cache = new MemoryCache(new MemoryCacheOptions());
        return new LineBindingService(context, logger.Object, cache);
    }

    /// <summary>
    /// 測試建立新綁定 - 應該成功建立並回傳綁定 ID
    /// </summary>
    [Fact]
    public async Task CreateOrUpdateBindingAsync_NewBinding_ReturnsBindingId()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var service = CreateService(context);

        // 先建立一個使用者
        var user = new User
        {
            DisplayName = "測試使用者",
            Email = "test@example.com",
            Role = UserRole.User,
            IsActive = true
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        // Act
        var bindingId = await service.CreateOrUpdateBindingAsync(
            user.Id,
            "U1234567890abcdef1234567890abcdef",
            "LINE 測試使用者",
            "https://profile.line-scdn.net/test.jpg"
        );

        // Assert
        bindingId.Should().BeGreaterThan(0);

        var binding = await context.LineBindings.FirstOrDefaultAsync(b => b.Id == bindingId);
        binding.Should().NotBeNull();
        binding!.UserId.Should().Be(user.Id);
        binding.LineUserId.Should().Be("U1234567890abcdef1234567890abcdef");
        binding.DisplayName.Should().Be("LINE 測試使用者");
        binding.PictureUrl.Should().Be("https://profile.line-scdn.net/test.jpg");
        binding.BindingStatus.Should().Be(BindingStatus.Active);
        binding.BoundAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        binding.LastInteractedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    /// <summary>
    /// 測試重複綁定相同 LINE User ID - 應該拋出 LineBindingException
    /// </summary>
    [Fact]
    public async Task CreateOrUpdateBindingAsync_DuplicateLineUserId_ThrowsException()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var service = CreateService(context);

        // 建立兩個使用者
        var user1 = new User { DisplayName = "使用者1", Email = "user1@example.com", Role = UserRole.User, IsActive = true };
        var user2 = new User { DisplayName = "使用者2", Email = "user2@example.com", Role = UserRole.User, IsActive = true };
        context.Users.AddRange(user1, user2);
        await context.SaveChangesAsync();

        var lineUserId = "U1234567890abcdef1234567890abcdef";

        // 先綁定第一個使用者
        await service.CreateOrUpdateBindingAsync(user1.Id, lineUserId, "LINE User");

        // Act & Assert
        var act = async () => await service.CreateOrUpdateBindingAsync(user2.Id, lineUserId, "LINE User");
        await act.Should().ThrowAsync<LineBindingException>()
            .WithMessage("*已被其他帳號綁定*");
    }

    /// <summary>
    /// 測試取得使用者綁定資訊 - 存在綁定應該回傳 DTO
    /// </summary>
    [Fact]
    public async Task GetBindingByUserIdAsync_ExistingBinding_ReturnsDto()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var service = CreateService(context);

        var user = new User { DisplayName = "測試使用者", Email = "test@example.com", Role = UserRole.User, IsActive = true };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var binding = new LineBinding
        {
            UserId = user.Id,
            LineUserId = "U1234567890abcdef1234567890abcdef",
            DisplayName = "LINE 測試使用者",
            PictureUrl = "https://profile.line-scdn.net/test.jpg",
            BindingStatus = BindingStatus.Active,
            BoundAt = DateTime.UtcNow,
            LastInteractedAt = DateTime.UtcNow
        };
        context.LineBindings.Add(binding);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetBindingByUserIdAsync(user.Id);

        // Assert
        result.Should().NotBeNull();
        result!.UserId.Should().Be(user.Id);
        result.LineUserId.Should().Be("U1234567890abcdef1234567890abcdef");
        result.DisplayName.Should().Be("LINE 測試使用者");
        result.BindingStatus.Should().Be(BindingStatus.Active);
    }

    /// <summary>
    /// 測試取得使用者綁定資訊 - 不存在綁定應該回傳 null
    /// </summary>
    [Fact]
    public async Task GetBindingByUserIdAsync_NoBinding_ReturnsNull()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var service = CreateService(context);

        var user = new User { DisplayName = "測試使用者", Email = "test@example.com", Role = UserRole.User, IsActive = true };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetBindingByUserIdAsync(user.Id);

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// 測試檢查使用者是否已綁定 - 已綁定且狀態為 Active 應該回傳 true
    /// </summary>
    [Fact]
    public async Task IsUserBoundAsync_BoundUser_ReturnsTrue()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var service = CreateService(context);

        var user = new User { DisplayName = "測試使用者", Email = "test@example.com", Role = UserRole.User, IsActive = true };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var binding = new LineBinding
        {
            UserId = user.Id,
            LineUserId = "U1234567890abcdef1234567890abcdef",
            DisplayName = "LINE 測試使用者",
            BindingStatus = BindingStatus.Active,
            BoundAt = DateTime.UtcNow,
            LastInteractedAt = DateTime.UtcNow
        };
        context.LineBindings.Add(binding);
        await context.SaveChangesAsync();

        // Act
        var result = await service.IsUserBoundAsync(user.Id);

        // Assert
        result.Should().BeTrue();
    }

    /// <summary>
    /// 測試檢查使用者是否已綁定 - 未綁定應該回傳 false
    /// </summary>
    [Fact]
    public async Task IsUserBoundAsync_UnboundUser_ReturnsFalse()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var service = CreateService(context);

        var user = new User { DisplayName = "測試使用者", Email = "test@example.com", Role = UserRole.User, IsActive = true };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        // Act
        var result = await service.IsUserBoundAsync(user.Id);

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// 測試檢查使用者是否已綁定 - 綁定狀態為 Blocked 應該回傳 false
    /// </summary>
    [Fact]
    public async Task IsUserBoundAsync_BlockedBinding_ReturnsFalse()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var service = CreateService(context);

        var user = new User { DisplayName = "測試使用者", Email = "test@example.com", Role = UserRole.User, IsActive = true };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var binding = new LineBinding
        {
            UserId = user.Id,
            LineUserId = "U1234567890abcdef1234567890abcdef",
            DisplayName = "LINE 測試使用者",
            BindingStatus = BindingStatus.Blocked,
            BoundAt = DateTime.UtcNow,
            LastInteractedAt = DateTime.UtcNow
        };
        context.LineBindings.Add(binding);
        await context.SaveChangesAsync();

        // Act
        var result = await service.IsUserBoundAsync(user.Id);

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// 測試解除綁定 - 存在綁定應該回傳 true
    /// </summary>
    [Fact]
    public async Task UnbindAsync_ExistingBinding_ReturnsTrue()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var service = CreateService(context);

        var user = new User { DisplayName = "測試使用者", Email = "test@example.com", Role = UserRole.User, IsActive = true };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var binding = new LineBinding
        {
            UserId = user.Id,
            LineUserId = "U1234567890abcdef1234567890abcdef",
            DisplayName = "LINE 測試使用者",
            BindingStatus = BindingStatus.Active,
            BoundAt = DateTime.UtcNow,
            LastInteractedAt = DateTime.UtcNow
        };
        context.LineBindings.Add(binding);
        await context.SaveChangesAsync();

        // Act
        var result = await service.UnbindAsync(user.Id);

        // Assert
        result.Should().BeTrue();
        
        var updatedBinding = await context.LineBindings.FirstOrDefaultAsync(b => b.UserId == user.Id);
        updatedBinding!.BindingStatus.Should().Be(BindingStatus.Unbound);
    }

    /// <summary>
    /// 測試解除綁定 - 不存在綁定應該回傳 false
    /// </summary>
    [Fact]
    public async Task UnbindAsync_NoBinding_ReturnsFalse()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var service = CreateService(context);

        var user = new User { DisplayName = "測試使用者", Email = "test@example.com", Role = UserRole.User, IsActive = true };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        // Act
        var result = await service.UnbindAsync(user.Id);

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// 測試標記為封鎖 - 應該更新狀態為 Blocked
    /// </summary>
    [Fact]
    public async Task MarkAsBlockedAsync_UpdatesStatus()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var service = CreateService(context);

        var user = new User { DisplayName = "測試使用者", Email = "test@example.com", Role = UserRole.User, IsActive = true };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var binding = new LineBinding
        {
            UserId = user.Id,
            LineUserId = "U1234567890abcdef1234567890abcdef",
            DisplayName = "LINE 測試使用者",
            BindingStatus = BindingStatus.Active,
            BoundAt = DateTime.UtcNow,
            LastInteractedAt = DateTime.UtcNow
        };
        context.LineBindings.Add(binding);
        await context.SaveChangesAsync();

        // Act
        await service.MarkAsBlockedAsync("U1234567890abcdef1234567890abcdef");

        // Assert
        var updatedBinding = await context.LineBindings
            .FirstOrDefaultAsync(b => b.LineUserId == "U1234567890abcdef1234567890abcdef");
        updatedBinding!.BindingStatus.Should().Be(BindingStatus.Blocked);
    }

    /// <summary>
    /// 測試標記為啟用 - 應該更新狀態為 Active
    /// </summary>
    [Fact]
    public async Task MarkAsActiveAsync_UpdatesStatus()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var service = CreateService(context);

        var user = new User { DisplayName = "測試使用者", Email = "test@example.com", Role = UserRole.User, IsActive = true };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var binding = new LineBinding
        {
            UserId = user.Id,
            LineUserId = "U1234567890abcdef1234567890abcdef",
            DisplayName = "LINE 測試使用者",
            BindingStatus = BindingStatus.Blocked,
            BoundAt = DateTime.UtcNow,
            LastInteractedAt = DateTime.UtcNow
        };
        context.LineBindings.Add(binding);
        await context.SaveChangesAsync();

        // Act
        await service.MarkAsActiveAsync("U1234567890abcdef1234567890abcdef");

        // Assert
        var updatedBinding = await context.LineBindings
            .FirstOrDefaultAsync(b => b.LineUserId == "U1234567890abcdef1234567890abcdef");
        updatedBinding!.BindingStatus.Should().Be(BindingStatus.Active);
    }

    /// <summary>
    /// 測試更新最後互動時間
    /// </summary>
    [Fact]
    public async Task UpdateLastInteractionAsync_UpdatesTimestamp()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var service = CreateService(context);

        var user = new User { DisplayName = "測試使用者", Email = "test@example.com", Role = UserRole.User, IsActive = true };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var oldInteractionTime = DateTime.UtcNow.AddHours(-1);
        var binding = new LineBinding
        {
            UserId = user.Id,
            LineUserId = "U1234567890abcdef1234567890abcdef",
            DisplayName = "LINE 測試使用者",
            BindingStatus = BindingStatus.Active,
            BoundAt = DateTime.UtcNow,
            LastInteractedAt = oldInteractionTime
        };
        context.LineBindings.Add(binding);
        await context.SaveChangesAsync();

        // Act
        await service.UpdateLastInteractionAsync("U1234567890abcdef1234567890abcdef");

        // Assert
        var updatedBinding = await context.LineBindings
            .FirstOrDefaultAsync(b => b.LineUserId == "U1234567890abcdef1234567890abcdef");
        updatedBinding!.LastInteractedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        updatedBinding.LastInteractedAt.Should().BeAfter(oldInteractionTime);
    }

    /// <summary>
    /// 測試訪客帳號綁定 - 應該拋出 InvalidOperationException
    /// </summary>
    [Fact]
    public async Task CreateOrUpdateBindingAsync_GuestUser_ThrowsInvalidOperationException()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var service = CreateService(context);

        var guestUser = new User
        {
            DisplayName = "訪客",
            Email = "guest@example.com",
            LineUserId = "guest", // 訪客帳號的特殊識別
            Role = UserRole.User,
            IsActive = true
        };
        context.Users.Add(guestUser);
        await context.SaveChangesAsync();

        // Act & Assert
        var act = async () => await service.CreateOrUpdateBindingAsync(
            guestUser.Id,
            "U1234567890abcdef1234567890abcdef",
            "LINE User"
        );
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*訪客帳號*");
    }
}
