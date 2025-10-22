using ClarityDesk.Data;
using ClarityDesk.Models.Entities;
using ClarityDesk.Models.Enums;
using ClarityDesk.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ClarityDesk.UnitTests.Services;

/// <summary>
/// UserManagementService 單元測試
/// </summary>
public class UserManagementServiceTests
{
    private ApplicationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    /// <summary>
    /// 測試取得所有使用者 - 應該回傳使用者清單
    /// </summary>
    [Fact]
    public async Task GetAllUsersAsync_ReturnsUserList()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var service = new UserManagementService(context, null!);

        var user1 = new User
        {
            LineUserId = "U001",
            DisplayName = "使用者1",
            Role = UserRole.User,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var user2 = new User
        {
            LineUserId = "U002",
            DisplayName = "使用者2",
            Role = UserRole.Admin,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Users.AddRange(user1, user2);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetAllUsersAsync(includeInactive: false);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(u => u.DisplayName == "使用者1");
        result.Should().Contain(u => u.DisplayName == "使用者2");
    }

    /// <summary>
    /// 測試更新使用者角色 - 有效的使用者 ID 應該回傳 true
    /// </summary>
    [Fact]
    public async Task UpdateUserRoleAsync_ValidUserId_ReturnsTrue()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var service = new UserManagementService(context, null!);

        var user = new User
        {
            LineUserId = "U001",
            DisplayName = "測試使用者",
            Role = UserRole.User,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        // Act
        var result = await service.UpdateUserRoleAsync(user.Id, UserRole.Admin);

        // Assert
        result.Should().BeTrue();

        var updatedUser = await context.Users.FindAsync(user.Id);
        updatedUser!.Role.Should().Be(UserRole.Admin);
    }

    /// <summary>
    /// 測試設定使用者啟用狀態 - 有效的使用者 ID 應該回傳 true
    /// </summary>
    [Fact]
    public async Task SetUserActiveStatusAsync_ValidUserId_ReturnsTrue()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var service = new UserManagementService(context, null!);

        var user = new User
        {
            LineUserId = "U001",
            DisplayName = "測試使用者",
            Role = UserRole.User,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        // Act
        var result = await service.SetUserActiveStatusAsync(user.Id, false);

        // Assert
        result.Should().BeTrue();

        var updatedUser = await context.Users.FindAsync(user.Id);
        updatedUser!.IsActive.Should().BeFalse();
    }

    /// <summary>
    /// 測試依角色取得使用者 - 管理員角色應該回傳管理員使用者
    /// </summary>
    [Fact]
    public async Task GetUsersByRoleAsync_AdminRole_ReturnsAdminUsers()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var service = new UserManagementService(context, null!);

        var adminUser = new User
        {
            LineUserId = "U_ADMIN",
            DisplayName = "管理員",
            Role = UserRole.Admin,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var normalUser = new User
        {
            LineUserId = "U_USER",
            DisplayName = "普通使用者",
            Role = UserRole.User,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Users.AddRange(adminUser, normalUser);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetUsersByRoleAsync(UserRole.Admin);

        // Assert
        result.Should().HaveCount(1);
        result.First().DisplayName.Should().Be("管理員");
        result.First().Role.Should().Be(UserRole.Admin);
    }
}
