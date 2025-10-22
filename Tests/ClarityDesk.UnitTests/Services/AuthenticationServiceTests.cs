using ClarityDesk.Data;
using ClarityDesk.Models.DTOs;
using ClarityDesk.Models.Entities;
using ClarityDesk.Models.Enums;
using ClarityDesk.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ClarityDesk.UnitTests.Services;

/// <summary>
/// AuthenticationService 單元測試
/// </summary>
public class AuthenticationServiceTests
{
    private ApplicationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }
    /// <summary>
    /// 測試 LINE 登入或註冊 - 新使用者應該建立使用者並回傳 DTO
    /// </summary>
    [Fact]
    public async Task LoginOrRegisterWithLineAsync_NewUser_CreatesUserAndReturnsDto()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var service = new AuthenticationService(context);
        
        var lineProfile = new LineUserProfileDto
        {
            UserId = "U1234567890abcdef",
            DisplayName = "測試使用者",
            PictureUrl = "https://profile.line-scdn.net/test.jpg",
            StatusMessage = "Hello"
        };

        // Act
        var result = await service.LoginOrRegisterWithLineAsync(lineProfile);

        // Assert
        result.Should().NotBeNull();
        result.LineUserId.Should().Be(lineProfile.UserId);
        result.DisplayName.Should().Be(lineProfile.DisplayName);
        result.Role.Should().Be(UserRole.User);
        result.IsActive.Should().BeTrue();
        
        // 驗證使用者已儲存至資料庫
        var savedUser = await context.Users.FirstOrDefaultAsync(u => u.LineUserId == lineProfile.UserId);
        savedUser.Should().NotBeNull();
    }

    /// <summary>
    /// 測試 LINE 登入或註冊 - 已存在使用者應該直接回傳 DTO
    /// </summary>
    [Fact]
    public async Task LoginOrRegisterWithLineAsync_ExistingUser_ReturnsDto()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var service = new AuthenticationService(context);
        
        var lineProfile = new LineUserProfileDto
        {
            UserId = "U1234567890abcdef",
            DisplayName = "已存在使用者",
            PictureUrl = "https://profile.line-scdn.net/test.jpg"
        };

        // 第一次登入建立使用者
        await service.LoginOrRegisterWithLineAsync(lineProfile);
        
        // 更新顯示名稱以測試更新功能
        lineProfile.DisplayName = "更新後的名稱";

        // Act
        var result = await service.LoginOrRegisterWithLineAsync(lineProfile);

        // Assert
        result.Should().NotBeNull();
        result.LineUserId.Should().Be(lineProfile.UserId);
        result.DisplayName.Should().Be("更新後的名稱");
        
        // 驗證只有一個使用者記錄
        var userCount = await context.Users.CountAsync(u => u.LineUserId == lineProfile.UserId);
        userCount.Should().Be(1);
    }

    /// <summary>
    /// 測試透過 LINE ID 取得使用者 - 存在的使用者應該回傳 DTO
    /// </summary>
    [Fact]
    public async Task GetUserByLineIdAsync_ExistingUser_ReturnsDto()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var service = new AuthenticationService(context);
        
        var user = new User
        {
            LineUserId = "U1234567890abcdef",
            DisplayName = "測試使用者",
            Role = UserRole.User,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        context.Users.Add(user);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetUserByLineIdAsync("U1234567890abcdef");

        // Assert
        result.Should().NotBeNull();
        result!.LineUserId.Should().Be("U1234567890abcdef");
        result.DisplayName.Should().Be("測試使用者");
    }

    /// <summary>
    /// 測試是否為管理員 - 管理員使用者應該回傳 true
    /// </summary>
    [Fact]
    public async Task IsAdminAsync_AdminUser_ReturnsTrue()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var service = new AuthenticationService(context);
        
        var adminUser = new User
        {
            LineUserId = "U_ADMIN",
            DisplayName = "管理員",
            Role = UserRole.Admin,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        context.Users.Add(adminUser);
        await context.SaveChangesAsync();

        // Act
        var result = await service.IsAdminAsync(adminUser.Id);

        // Assert
        result.Should().BeTrue();
    }

    /// <summary>
    /// 測試使用者是否啟用 - 啟用的使用者應該回傳 true
    /// </summary>
    [Fact]
    public async Task IsUserActiveAsync_ActiveUser_ReturnsTrue()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var service = new AuthenticationService(context);
        
        var activeUser = new User
        {
            LineUserId = "U_ACTIVE",
            DisplayName = "啟用使用者",
            Role = UserRole.User,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        context.Users.Add(activeUser);
        await context.SaveChangesAsync();

        // Act
        var result = await service.IsUserActiveAsync(activeUser.Id);

        // Assert
        result.Should().BeTrue();
    }

    /// <summary>
    /// 測試遊客登入 - 首次登入應該建立遊客帳號
    /// </summary>
    [Fact]
    public async Task LoginAsGuestAsync_FirstTime_CreatesGuestUser()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var service = new AuthenticationService(context);

        // Act
        var result = await service.LoginAsGuestAsync();

        // Assert
        result.Should().NotBeNull();
        result.LineUserId.Should().Be("guest");
        result.DisplayName.Should().Be("訪客");
        result.Role.Should().Be(UserRole.User);
        result.IsActive.Should().BeTrue();

        // 驗證遊客使用者已儲存至資料庫
        var guestUser = await context.Users.FirstOrDefaultAsync(u => u.LineUserId == "guest");
        guestUser.Should().NotBeNull();
    }

    /// <summary>
    /// 測試遊客登入 - 已存在遊客帳號應該回傳現有帳號
    /// </summary>
    [Fact]
    public async Task LoginAsGuestAsync_ExistingGuest_ReturnsExistingUser()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var service = new AuthenticationService(context);

        // 第一次登入建立遊客帳號
        await service.LoginAsGuestAsync();

        // Act - 第二次登入
        var result = await service.LoginAsGuestAsync();

        // Assert
        result.Should().NotBeNull();
        result.LineUserId.Should().Be("guest");
        
        // 驗證只有一個遊客帳號
        var guestCount = await context.Users.CountAsync(u => u.LineUserId == "guest");
        guestCount.Should().Be(1);
    }
}
