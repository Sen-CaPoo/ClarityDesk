using ClarityDesk.Data;
using ClarityDesk.Models.DTOs;
using ClarityDesk.Models.Entities;
using ClarityDesk.Models.Enums;
using ClarityDesk.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Xunit;

namespace ClarityDesk.UnitTests.Services;

/// <summary>
/// DepartmentService 單元測試
/// </summary>
public class DepartmentServiceTests
{
    private ApplicationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private IMemoryCache CreateMemoryCache()
    {
        return new MemoryCache(new MemoryCacheOptions());
    }

    /// <summary>
    /// 測試建立單位 - 有效的 DTO 應該回傳單位 ID
    /// </summary>
    [Fact]
    public async Task CreateDepartmentAsync_ValidDto_ReturnsDepartmentId()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var cache = CreateMemoryCache();
        var service = new DepartmentService(context, cache);

        var createDto = new CreateDepartmentDto
        {
            Name = "測試單位",
            Description = "這是一個測試單位"
        };

        // Act
        var result = await service.CreateDepartmentAsync(createDto);

        // Assert
        result.Should().BeGreaterThan(0);

        var department = await context.Departments.FindAsync(result);
        department.Should().NotBeNull();
        department!.Name.Should().Be("測試單位");
        department.Description.Should().Be("這是一個測試單位");
        department.IsActive.Should().BeTrue();
    }

    /// <summary>
    /// 測試更新單位 - 有效的 DTO 應該回傳 true
    /// </summary>
    [Fact]
    public async Task UpdateDepartmentAsync_ValidDto_ReturnsTrue()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var cache = CreateMemoryCache();
        var service = new DepartmentService(context, cache);

        var department = new Department
        {
            Name = "原始名稱",
            Description = "原始說明",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Departments.Add(department);
        await context.SaveChangesAsync();

        var updateDto = new UpdateDepartmentDto
        {
            Name = "更新後的名稱",
            Description = "更新後的說明",
            IsActive = true
        };

        // Act
        var result = await service.UpdateDepartmentAsync(department.Id, updateDto);

        // Assert
        result.Should().BeTrue();

        var updatedDepartment = await context.Departments.FindAsync(department.Id);
        updatedDepartment!.Name.Should().Be("更新後的名稱");
        updatedDepartment.Description.Should().Be("更新後的說明");
    }

    /// <summary>
    /// 測試刪除單位 - 存在的 ID 應該進行軟刪除
    /// </summary>
    [Fact]
    public async Task DeleteDepartmentAsync_ExistingId_SoftDeletes()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var cache = CreateMemoryCache();
        var service = new DepartmentService(context, cache);

        var department = new Department
        {
            Name = "要刪除的單位",
            Description = "測試軟刪除",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Departments.Add(department);
        await context.SaveChangesAsync();

        // Act
        var result = await service.DeleteDepartmentAsync(department.Id);

        // Assert
        result.Should().BeTrue();

        var deletedDepartment = await context.Departments.FindAsync(department.Id);
        deletedDepartment.Should().NotBeNull();
        deletedDepartment!.IsActive.Should().BeFalse(); // 軟刪除,IsActive 設為 false
    }

    /// <summary>
    /// 測試取得所有單位 - 僅啟用時應該只回傳啟用的單位
    /// </summary>
    [Fact]
    public async Task GetAllDepartmentsAsync_ActiveOnly_ReturnsActiveDepartments()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var cache = CreateMemoryCache();
        var service = new DepartmentService(context, cache);

        var activeDept = new Department
        {
            Name = "啟用單位",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var inactiveDept = new Department
        {
            Name = "停用單位",
            IsActive = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Departments.AddRange(activeDept, inactiveDept);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetAllDepartmentsAsync(activeOnly: true);

        // Assert
        result.Should().HaveCount(1);
        result.First().Name.Should().Be("啟用單位");
    }

    /// <summary>
    /// 測試指派使用者到單位 - 有效的使用者 ID 清單應該回傳 true
    /// </summary>
    [Fact]
    public async Task AssignUsersToDepartmentAsync_ValidUserIds_ReturnsTrue()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var cache = CreateMemoryCache();
        var service = new DepartmentService(context, cache);

        var department = new Department
        {
            Name = "測試單位",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

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
            Role = UserRole.User,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Departments.Add(department);
        context.Users.AddRange(user1, user2);
        await context.SaveChangesAsync();

        // Act
        var result = await service.AssignUsersToDepartmentAsync(department.Id, new List<int> { user1.Id, user2.Id });

        // Assert
        result.Should().BeTrue();

        var assignments = await context.DepartmentUsers
            .Where(du => du.DepartmentId == department.Id)
            .ToListAsync();

        assignments.Should().HaveCount(2);
    }
}
