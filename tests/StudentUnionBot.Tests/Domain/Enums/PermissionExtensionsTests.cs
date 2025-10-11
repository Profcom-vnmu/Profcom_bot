using FluentAssertions;
using StudentUnionBot.Domain.Enums;
using Xunit;

namespace StudentUnionBot.Tests.Domain.Enums;

public class PermissionExtensionsTests
{
    #region GetPermissions Tests

    [Fact]
    public void GetPermissions_ForStudentRole_ShouldReturnStudentPermissions()
    {
        // Act
        var permissions = UserRole.Student.GetPermissions();

        // Assert
        permissions.Should().NotBeEmpty();
        permissions.Should().Contain(Permission.ViewProfile);
        permissions.Should().Contain(Permission.EditProfile);
        permissions.Should().Contain(Permission.CreateAppeal);
        permissions.Should().Contain(Permission.ViewNews);
        permissions.Should().Contain(Permission.ViewEvents);
        permissions.Should().Contain(Permission.RegisterForEvent);
        
        // Student should NOT have these permissions
        permissions.Should().NotContain(Permission.CreateNews);
        permissions.Should().NotContain(Permission.DeleteNews);
        permissions.Should().NotContain(Permission.ManageUsers);
        permissions.Should().NotContain(Permission.ManageSystem);
    }

    [Fact]
    public void GetPermissions_ForModeratorRole_ShouldIncludeModeratorPermissions()
    {
        // Act
        var permissions = UserRole.Moderator.GetPermissions();

        // Assert
        permissions.Should().NotBeEmpty();
        
        // Should have Student permissions
        permissions.Should().Contain(Permission.ViewNews);
        permissions.Should().Contain(Permission.ViewEvents);
        
        // Should have Moderator-specific permissions
        permissions.Should().Contain(Permission.CreateNews);
        permissions.Should().Contain(Permission.EditNews);
        permissions.Should().Contain(Permission.CreateEvent);
        permissions.Should().Contain(Permission.ViewAppeals);
        permissions.Should().Contain(Permission.ReplyToAppeal);
        
        // Should NOT have Admin permissions
        permissions.Should().NotContain(Permission.DeleteNews);
        permissions.Should().NotContain(Permission.ManageUsers);
        permissions.Should().NotContain(Permission.BanUsers);
    }

    [Fact]
    public void GetPermissions_ForAdminRole_ShouldIncludeAdminPermissions()
    {
        // Act
        var permissions = UserRole.Admin.GetPermissions();

        // Assert
        permissions.Should().NotBeEmpty();
        
        // Should have Student permissions
        permissions.Should().Contain(Permission.ViewNews);
        
        // Should have Moderator permissions
        permissions.Should().Contain(Permission.CreateNews);
        permissions.Should().Contain(Permission.EditNews);
        
        // Should have Admin-specific permissions
        permissions.Should().Contain(Permission.DeleteNews);
        permissions.Should().Contain(Permission.PublishNews);
        permissions.Should().Contain(Permission.AssignAppeal);
        permissions.Should().Contain(Permission.ManageUsers);
        permissions.Should().Contain(Permission.BanUsers);
        permissions.Should().Contain(Permission.CreateBackup);
        
        // Should have most permissions but not ALL (reserved for SuperAdmin)
        var allPermissions = Enum.GetValues<Permission>();
        permissions.Count.Should().BeLessThan(allPermissions.Length);
    }

    [Fact]
    public void GetPermissions_ForSuperAdminRole_ShouldIncludeAllPermissions()
    {
        // Act
        var permissions = UserRole.SuperAdmin.GetPermissions();
        var allPermissions = Enum.GetValues<Permission>();

        // Assert
        permissions.Should().NotBeEmpty();
        permissions.Should().HaveCount(allPermissions.Length);
        permissions.Should().Contain(allPermissions);
    }

    #endregion

    #region HasPermission Tests

    [Fact]
    public void HasPermission_StudentWithViewNews_ShouldReturnTrue()
    {
        // Act
        var result = UserRole.Student.HasPermission(Permission.ViewNews);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void HasPermission_StudentWithCreateNews_ShouldReturnFalse()
    {
        // Act
        var result = UserRole.Student.HasPermission(Permission.CreateNews);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void HasPermission_ModeratorWithCreateNews_ShouldReturnTrue()
    {
        // Act
        var result = UserRole.Moderator.HasPermission(Permission.CreateNews);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void HasPermission_ModeratorWithDeleteNews_ShouldReturnFalse()
    {
        // Act
        var result = UserRole.Moderator.HasPermission(Permission.DeleteNews);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void HasPermission_AdminWithManageUsers_ShouldReturnTrue()
    {
        // Act
        var result = UserRole.Admin.HasPermission(Permission.ManageUsers);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void HasPermission_SuperAdminWithAnyPermission_ShouldReturnTrue()
    {
        // Act & Assert
        UserRole.SuperAdmin.HasPermission(Permission.ManageSystem).Should().BeTrue();
        UserRole.SuperAdmin.HasPermission(Permission.ViewLogs).Should().BeTrue();
        UserRole.SuperAdmin.HasPermission(Permission.SendBroadcast).Should().BeTrue();
    }

    #endregion

    #region HasAnyPermission Tests

    [Fact]
    public void HasAnyPermission_StudentWithMultiplePermissions_ShouldReturnTrueIfHasAny()
    {
        // Act
        var result = UserRole.Student.HasAnyPermission(
            Permission.CreateNews, 
            Permission.ViewNews,  // Student has this
            Permission.ManageUsers);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void HasAnyPermission_StudentWithNoMatchingPermissions_ShouldReturnFalse()
    {
        // Act
        var result = UserRole.Student.HasAnyPermission(
            Permission.CreateNews,
            Permission.DeleteNews,
            Permission.ManageUsers);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void HasAnyPermission_ModeratorWithMixedPermissions_ShouldReturnTrue()
    {
        // Act
        var result = UserRole.Moderator.HasAnyPermission(
            Permission.ManageSystem,  // Moderator doesn't have
            Permission.CreateNews);   // Moderator has this

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void HasAnyPermission_WithEmptyArray_ShouldReturnFalse()
    {
        // Act
        var result = UserRole.Admin.HasAnyPermission();

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region HasAllPermissions Tests

    [Fact]
    public void HasAllPermissions_StudentWithAllStudentPermissions_ShouldReturnTrue()
    {
        // Act
        var result = UserRole.Student.HasAllPermissions(
            Permission.ViewNews,
            Permission.ViewEvents,
            Permission.CreateAppeal);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void HasAllPermissions_StudentWithMixedPermissions_ShouldReturnFalse()
    {
        // Act
        var result = UserRole.Student.HasAllPermissions(
            Permission.ViewNews,    // Student has
            Permission.CreateNews); // Student doesn't have

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void HasAllPermissions_AdminWithAllAdminPermissions_ShouldReturnTrue()
    {
        // Act
        var result = UserRole.Admin.HasAllPermissions(
            Permission.CreateNews,
            Permission.EditNews,
            Permission.DeleteNews,
            Permission.ManageUsers);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void HasAllPermissions_ModeratorWithAdminPermissions_ShouldReturnFalse()
    {
        // Act
        var result = UserRole.Moderator.HasAllPermissions(
            Permission.CreateNews,  // Moderator has
            Permission.DeleteNews); // Moderator doesn't have

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void HasAllPermissions_WithEmptyArray_ShouldReturnTrue()
    {
        // Act
        var result = UserRole.Student.HasAllPermissions();

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region GetDisplayName Tests

    [Fact]
    public void GetDisplayName_ForViewProfile_ShouldReturnUkrainianName()
    {
        // Act
        var displayName = Permission.ViewProfile.GetDisplayName();

        // Assert
        displayName.Should().Be("Перегляд профілю");
    }

    [Fact]
    public void GetDisplayName_ForCreateNews_ShouldReturnUkrainianName()
    {
        // Act
        var displayName = Permission.CreateNews.GetDisplayName();

        // Assert
        displayName.Should().Be("Створення новин");
    }

    [Fact]
    public void GetDisplayName_ForManageUsers_ShouldReturnUkrainianName()
    {
        // Act
        var displayName = Permission.ManageUsers.GetDisplayName();

        // Assert
        displayName.Should().Be("Управління користувачами");
    }

    [Fact]
    public void GetDisplayName_ForSendBroadcast_ShouldReturnEnumName()
    {
        // Act
        var displayName = Permission.SendBroadcast.GetDisplayName();

        // Assert
        displayName.Should().NotBeNullOrEmpty();
        // Should return enum name if not in switch statement, or proper Ukrainian name if defined
    }

    [Fact]
    public void GetDisplayName_AllPermissionsShouldHaveDisplayName()
    {
        // Arrange
        var allPermissions = Enum.GetValues<Permission>();

        // Act & Assert
        foreach (var permission in allPermissions)
        {
            var displayName = permission.GetDisplayName();
            displayName.Should().NotBeNullOrWhiteSpace();
        }
    }

    #endregion

    #region Permission Hierarchy Tests

    [Fact]
    public void StudentPermissions_ShouldBeSubsetOfModeratorPermissions()
    {
        // Arrange
        var studentPermissions = UserRole.Student.GetPermissions();
        var moderatorPermissions = UserRole.Moderator.GetPermissions();

        // Assert
        studentPermissions.All(p => moderatorPermissions.Contains(p)).Should().BeTrue();
    }

    [Fact]
    public void ModeratorPermissions_ShouldBeSubsetOfAdminPermissions()
    {
        // Arrange
        var moderatorPermissions = UserRole.Moderator.GetPermissions();
        var adminPermissions = UserRole.Admin.GetPermissions();

        // Assert
        moderatorPermissions.All(p => adminPermissions.Contains(p)).Should().BeTrue();
    }

    [Fact]
    public void AdminPermissions_ShouldBeSubsetOfSuperAdminPermissions()
    {
        // Arrange
        var adminPermissions = UserRole.Admin.GetPermissions();
        var superAdminPermissions = UserRole.SuperAdmin.GetPermissions();

        // Assert
        adminPermissions.All(p => superAdminPermissions.Contains(p)).Should().BeTrue();
    }

    [Fact]
    public void PermissionCount_ShouldIncreaseWithRoleLevel()
    {
        // Arrange
        var studentCount = UserRole.Student.GetPermissions().Count;
        var moderatorCount = UserRole.Moderator.GetPermissions().Count;
        var adminCount = UserRole.Admin.GetPermissions().Count;
        var superAdminCount = UserRole.SuperAdmin.GetPermissions().Count;

        // Assert
        studentCount.Should().BeLessThan(moderatorCount);
        moderatorCount.Should().BeLessThan(adminCount);
        adminCount.Should().BeLessThan(superAdminCount);
    }

    #endregion
}
