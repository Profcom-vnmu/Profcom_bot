using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using StudentUnionBot.Domain.Entities;
using StudentUnionBot.Domain.Enums;
using StudentUnionBot.Domain.Interfaces;
using StudentUnionBot.Infrastructure.Services;
using Xunit;

namespace StudentUnionBot.Tests.Infrastructure.Services;

public class AuthorizationServiceTests
{
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<ILogger<AuthorizationService>> _mockLogger;
    private readonly AuthorizationService _service;

    public AuthorizationServiceTests()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockLogger = new Mock<ILogger<AuthorizationService>>();
        _service = new AuthorizationService(_mockUserRepository.Object, _mockLogger.Object);
    }

    private static BotUser CreateUser(long telegramId, UserRole role)
    {
        var user = BotUser.Create(telegramId, "testuser", "Test", "User", Language.Ukrainian);
        if (role != UserRole.Student) // Student is default
        {
            user.PromoteToRole(role);
        }
        return user;
    }

    #region HasPermissionAsync Tests

    [Fact]
    public async Task HasPermissionAsync_WithStudentRole_AndViewNewsPermission_ShouldReturnTrue()
    {
        // Arrange
        var userId = 123L;
        var user = CreateUser(userId, UserRole.Student);

        _mockUserRepository
            .Setup(x => x.GetByTelegramIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _service.HasPermissionAsync(userId, Permission.ViewNews);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasPermissionAsync_WithStudentRole_AndCreateNewsPermission_ShouldReturnFalse()
    {
        // Arrange
        var userId = 123L;
        var user = CreateUser(userId, UserRole.Student);

        _mockUserRepository
            .Setup(x => x.GetByTelegramIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _service.HasPermissionAsync(userId, Permission.CreateNews);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task HasPermissionAsync_WithModeratorRole_AndCreateNewsPermission_ShouldReturnTrue()
    {
        // Arrange
        var userId = 123L;
        var user = CreateUser(userId, UserRole.Moderator);

        _mockUserRepository
            .Setup(x => x.GetByTelegramIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _service.HasPermissionAsync(userId, Permission.CreateNews);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasPermissionAsync_WithAdminRole_AndDeleteNewsPermission_ShouldReturnTrue()
    {
        // Arrange
        var userId = 123L;
        var user = CreateUser(userId, UserRole.Admin);

        _mockUserRepository
            .Setup(x => x.GetByTelegramIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _service.HasPermissionAsync(userId, Permission.DeleteNews);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasPermissionAsync_WithSuperAdminRole_AndAnyPermission_ShouldReturnTrue()
    {
        // Arrange
        var userId = 123L;
        var user = CreateUser(userId, UserRole.SuperAdmin);

        _mockUserRepository
            .Setup(x => x.GetByTelegramIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result1 = await _service.HasPermissionAsync(userId, Permission.ManageSystem);
        var result2 = await _service.HasPermissionAsync(userId, Permission.ViewLogs);
        var result3 = await _service.HasPermissionAsync(userId, Permission.PromoteUsers);

        // Assert
        result1.Should().BeTrue();
        result2.Should().BeTrue();
        result3.Should().BeTrue();
    }

    [Fact]
    public async Task HasPermissionAsync_WithNonExistentUser_ShouldReturnFalse()
    {
        // Arrange
        var userId = 999L;
        _mockUserRepository
            .Setup(x => x.GetByTelegramIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BotUser?)null);

        // Act
        var result = await _service.HasPermissionAsync(userId, Permission.ViewNews);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task HasPermissionAsync_WhenRepositoryThrowsException_ShouldReturnFalse()
    {
        // Arrange
        var userId = 123L;
        _mockUserRepository
            .Setup(x => x.GetByTelegramIdAsync(userId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _service.HasPermissionAsync(userId, Permission.ViewNews);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region HasAnyPermissionAsync Tests

    [Fact]
    public async Task HasAnyPermissionAsync_WithStudentRole_AndStudentPermissions_ShouldReturnTrue()
    {
        // Arrange
        var userId = 123L;
        var user = CreateUser(userId, UserRole.Student);

        _mockUserRepository
            .Setup(x => x.GetByTelegramIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _service.HasAnyPermissionAsync(
            userId, 
            default, 
            Permission.ViewNews, 
            Permission.CreateNews);

        // Assert
        result.Should().BeTrue(); // Has ViewNews
    }

    [Fact]
    public async Task HasAnyPermissionAsync_WithStudentRole_AndNoMatchingPermissions_ShouldReturnFalse()
    {
        // Arrange
        var userId = 123L;
        var user = CreateUser(userId, UserRole.Student);

        _mockUserRepository
            .Setup(x => x.GetByTelegramIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _service.HasAnyPermissionAsync(
            userId, 
            default, 
            Permission.CreateNews, 
            Permission.DeleteNews,
            Permission.ManageSystem);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task HasAnyPermissionAsync_WithEmptyPermissions_ShouldReturnFalse()
    {
        // Arrange
        var userId = 123L;
        var user = CreateUser(userId, UserRole.Student);

        _mockUserRepository
            .Setup(x => x.GetByTelegramIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _service.HasAnyPermissionAsync(userId, default);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task HasAnyPermissionAsync_WhenUserNotFound_ShouldReturnFalse()
    {
        // Arrange
        var userId = 999L;
        _mockUserRepository
            .Setup(x => x.GetByTelegramIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BotUser?)null);

        // Act
        var result = await _service.HasAnyPermissionAsync(
            userId, 
            default, 
            Permission.ViewNews);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region HasAllPermissionsAsync Tests

    [Fact]
    public async Task HasAllPermissionsAsync_WithAdminRole_AndAllAdminPermissions_ShouldReturnTrue()
    {
        // Arrange
        var userId = 123L;
        var user = CreateUser(userId, UserRole.Admin);

        _mockUserRepository
            .Setup(x => x.GetByTelegramIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _service.HasAllPermissionsAsync(
            userId, 
            default, 
            Permission.CreateNews, 
            Permission.EditNews,
            Permission.DeleteNews);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasAllPermissionsAsync_WithModeratorRole_AndMixedPermissions_ShouldReturnFalse()
    {
        // Arrange
        var userId = 123L;
        var user = CreateUser(userId, UserRole.Moderator);

        _mockUserRepository
            .Setup(x => x.GetByTelegramIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _service.HasAllPermissionsAsync(
            userId, 
            default, 
            Permission.CreateNews,  // Moderator has this
            Permission.DeleteNews); // Moderator doesn't have this

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task HasAllPermissionsAsync_WithEmptyPermissions_ShouldReturnTrue()
    {
        // Arrange
        var userId = 123L;
        var user = CreateUser(userId, UserRole.Student);

        _mockUserRepository
            .Setup(x => x.GetByTelegramIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _service.HasAllPermissionsAsync(userId, default);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasAllPermissionsAsync_WhenUserNotFound_ShouldReturnFalse()
    {
        // Arrange
        var userId = 999L;
        _mockUserRepository
            .Setup(x => x.GetByTelegramIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BotUser?)null);

        // Act
        var result = await _service.HasAllPermissionsAsync(
            userId, 
            default, 
            Permission.ViewNews);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region GetUserPermissionsAsync Tests

    [Fact]
    public async Task GetUserPermissionsAsync_WithStudentRole_ShouldReturnStudentPermissions()
    {
        // Arrange
        var userId = 123L;
        var user = CreateUser(userId, UserRole.Student);

        _mockUserRepository
            .Setup(x => x.GetByTelegramIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _service.GetUserPermissionsAsync(userId);

        // Assert
        result.Should().NotBeEmpty();
        result.Should().Contain(Permission.ViewNews);
        result.Should().Contain(Permission.ViewEvents);
        result.Should().Contain(Permission.CreateAppeal);
        result.Should().NotContain(Permission.CreateNews);
        result.Should().NotContain(Permission.ManageSystem);
    }

    [Fact]
    public async Task GetUserPermissionsAsync_WithAdminRole_ShouldReturnAdminPermissions()
    {
        // Arrange
        var userId = 123L;
        var user = CreateUser(userId, UserRole.Admin);

        _mockUserRepository
            .Setup(x => x.GetByTelegramIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _service.GetUserPermissionsAsync(userId);

        // Assert
        result.Should().NotBeEmpty();
        result.Should().Contain(Permission.CreateNews);
        result.Should().Contain(Permission.DeleteNews);
        result.Should().Contain(Permission.ManageUsers);
        result.Should().Contain(Permission.AssignAppeal);
    }

    [Fact]
    public async Task GetUserPermissionsAsync_WhenUserNotFound_ShouldReturnEmptyList()
    {
        // Arrange
        var userId = 999L;
        _mockUserRepository
            .Setup(x => x.GetByTelegramIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BotUser?)null);

        // Act
        var result = await _service.GetUserPermissionsAsync(userId);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetUserPermissionsAsync_WhenExceptionOccurs_ShouldReturnEmptyList()
    {
        // Arrange
        var userId = 123L;
        _mockUserRepository
            .Setup(x => x.GetByTelegramIdAsync(userId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _service.GetUserPermissionsAsync(userId);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region GetUserRoleAsync Tests

    [Fact]
    public async Task GetUserRoleAsync_WithExistingUser_ShouldReturnRole()
    {
        // Arrange
        var userId = 123L;
        var user = CreateUser(userId, UserRole.Moderator);

        _mockUserRepository
            .Setup(x => x.GetByTelegramIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _service.GetUserRoleAsync(userId);

        // Assert
        result.Should().Be(UserRole.Moderator);
    }

    [Fact]
    public async Task GetUserRoleAsync_WithNonExistentUser_ShouldReturnNull()
    {
        // Arrange
        var userId = 999L;
        _mockUserRepository
            .Setup(x => x.GetByTelegramIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BotUser?)null);

        // Act
        var result = await _service.GetUserRoleAsync(userId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region IsAdminAsync Tests

    [Fact]
    public async Task IsAdminAsync_WithAdminRole_ShouldReturnTrue()
    {
        // Arrange
        var userId = 123L;
        var user = CreateUser(userId, UserRole.Admin);

        _mockUserRepository
            .Setup(x => x.GetByTelegramIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _service.IsAdminAsync(userId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsAdminAsync_WithSuperAdminRole_ShouldReturnTrue()
    {
        // Arrange
        var userId = 123L;
        var user = CreateUser(userId, UserRole.SuperAdmin);

        _mockUserRepository
            .Setup(x => x.GetByTelegramIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _service.IsAdminAsync(userId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsAdminAsync_WithModeratorRole_ShouldReturnFalse()
    {
        // Arrange
        var userId = 123L;
        var user = CreateUser(userId, UserRole.Moderator);

        _mockUserRepository
            .Setup(x => x.GetByTelegramIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _service.IsAdminAsync(userId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsAdminAsync_WithStudentRole_ShouldReturnFalse()
    {
        // Arrange
        var userId = 123L;
        var user = CreateUser(userId, UserRole.Student);

        _mockUserRepository
            .Setup(x => x.GetByTelegramIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _service.IsAdminAsync(userId);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region IsSuperAdminAsync Tests

    [Fact]
    public async Task IsSuperAdminAsync_WithSuperAdminRole_ShouldReturnTrue()
    {
        // Arrange
        var userId = 123L;
        var user = CreateUser(userId, UserRole.SuperAdmin);

        _mockUserRepository
            .Setup(x => x.GetByTelegramIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _service.IsSuperAdminAsync(userId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsSuperAdminAsync_WithAdminRole_ShouldReturnFalse()
    {
        // Arrange
        var userId = 123L;
        var user = CreateUser(userId, UserRole.Admin);

        _mockUserRepository
            .Setup(x => x.GetByTelegramIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _service.IsSuperAdminAsync(userId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsSuperAdminAsync_WithModeratorRole_ShouldReturnFalse()
    {
        // Arrange
        var userId = 123L;
        var user = CreateUser(userId, UserRole.Moderator);

        _mockUserRepository
            .Setup(x => x.GetByTelegramIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _service.IsSuperAdminAsync(userId);

        // Assert
        result.Should().BeFalse();
    }

    #endregion
}
