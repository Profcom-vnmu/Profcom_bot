using FluentAssertions;
using StudentUnionBot.Core.Exceptions;
using StudentUnionBot.Domain.Entities;
using StudentUnionBot.Domain.Enums;
using StudentUnionBot.Tests.Helpers;
using Xunit;

namespace StudentUnionBot.Tests.Domain.Entities;

/// <summary>
/// Тести для сутності BotUser
/// </summary>
public class BotUserTests : TestBase
{
    [Fact]
    public void Create_WithValidData_ShouldCreateUser()
    {
        // Arrange
        var telegramId = 123456789L;
        var username = "testuser";
        var firstName = "John";
        var lastName = "Doe";

        // Act
        var user = BotUser.Create(telegramId, username, firstName, lastName);

        // Assert
        user.Should().NotBeNull();
        user.TelegramId.Should().Be(telegramId);
        user.Username.Should().Be(username);
        user.FirstName.Should().Be(firstName);
        user.LastName.Should().Be(lastName);
        user.FullName.Should().Be("John Doe");
        user.Role.Should().Be(UserRole.Student);
        user.Language.Should().Be(Language.Ukrainian);
        user.IsActive.Should().BeTrue();
        user.IsBanned.Should().BeFalse();
        user.JoinedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-999)]
    public void Create_WithInvalidTelegramId_ShouldThrowException(long invalidId)
    {
        // Arrange & Act
        Action act = () => BotUser.Create(invalidId, "testuser", "John", "Doe");

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*Telegram ID*");
    }

    [Fact]
    public void Create_WithNullLastName_ShouldCreateUserWithFirstNameOnly()
    {
        // Arrange & Act
        var user = BotUser.Create(123456789, "testuser", "John", null);

        // Assert
        user.FullName.Should().Be("John");
        user.LastName.Should().BeNull();
    }

    [Fact]
    public void UpdateProfile_WithValidData_ShouldUpdateUser()
    {
        // Arrange
        var user = CreateTestUser();
        var faculty = "Computer Science";
        var course = 3;
        var group = "CS-301";
        var email = "test@example.com";

        // Act
        user.UpdateProfile(faculty, course, group, email);

        // Assert
        user.Faculty.Should().Be(faculty);
        user.Course.Should().Be(course);
        user.Group.Should().Be(group);
        user.Email.Should().Be(email);
        user.IsEmailVerified.Should().BeFalse(); // Новий email потребує верифікації
        user.ProfileUpdatedAt.Should().NotBeNull();
        user.ProfileUpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(7)]
    [InlineData(10)]
    public void UpdateProfile_WithInvalidCourse_ShouldThrowException(int invalidCourse)
    {
        // Arrange
        var user = CreateTestUser();

        // Act
        Action act = () => user.UpdateProfile("CS", invalidCourse, "CS-301");

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*курс*");
    }

    [Fact]
    public void GenerateVerificationCode_ShouldCreateSixDigitCode()
    {
        // Arrange
        var user = CreateTestUser();
        user.SetEmailForVerification("test@example.com");

        // Act
        var code = user.GenerateVerificationCode();

        // Assert
        code.Should().NotBeNullOrEmpty();
        code.Length.Should().Be(6);
        code.Should().MatchRegex(@"^\d{6}$"); // 6 digits
        user.VerificationCodeExpiry.Should().NotBeNull();
        user.VerificationCodeExpiry.Should().BeCloseTo(
            DateTime.UtcNow.AddMinutes(15), 
            TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void VerifyEmail_WithCorrectCode_ShouldVerifyEmail()
    {
        // Arrange
        var user = CreateTestUser();
        user.SetEmailForVerification("test@example.com");
        var code = user.GenerateVerificationCode();

        // Act
        var result = user.VerifyEmail(code);

        // Assert
        result.Should().BeTrue();
        user.IsEmailVerified.Should().BeTrue();
        user.VerificationCode.Should().BeNull();
        user.VerificationCodeExpiry.Should().BeNull();
    }

    [Fact]
    public void VerifyEmail_WithIncorrectCode_ShouldNotVerify()
    {
        // Arrange
        var user = CreateTestUser();
        user.SetEmailForVerification("test@example.com");
        user.GenerateVerificationCode();

        // Act
        var result = user.VerifyEmail("000000");

        // Assert
        result.Should().BeFalse();
        user.IsEmailVerified.Should().BeFalse();
    }

    [Fact]
    public void Ban_WithReason_ShouldBanUser()
    {
        // Arrange
        var user = CreateTestUser();
        var reason = "Spam detected";

        // Act
        user.Ban(reason);

        // Assert
        user.IsBanned.Should().BeTrue();
        user.BanReason.Should().Be(reason);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Ban_WithEmptyReason_ShouldThrowException(string? emptyReason)
    {
        // Arrange
        var user = CreateTestUser();

        // Act
        Action act = () => user.Ban(emptyReason!);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*причина*");
    }

    [Fact]
    public void Unban_ShouldUnbanUser()
    {
        // Arrange
        var user = CreateTestUser();
        user.Ban("Test ban");

        // Act
        user.Unban();

        // Assert
        user.IsBanned.Should().BeFalse();
        user.BanReason.Should().BeNull();
    }

    [Fact]
    public void SetLanguage_ShouldChangeLanguage()
    {
        // Arrange
        var user = CreateTestUser(language: Language.Ukrainian);

        // Act
        user.SetLanguage(Language.English);

        // Assert
        user.Language.Should().Be(Language.English);
    }

    [Fact]
    public void PromoteToRole_WithValidRole_ShouldChangeRole()
    {
        // Arrange
        var user = CreateTestUser();

        // Act
        user.PromoteToRole(UserRole.Admin);

        // Assert
        user.Role.Should().Be(UserRole.Admin);
    }

    [Fact]
    public void PromoteToRole_FromSuperAdminToLowerRole_ShouldThrowException()
    {
        // Arrange
        var user = CreateTestUser();
        user.PromoteToRole(UserRole.SuperAdmin);

        // Act
        Action act = () => user.PromoteToRole(UserRole.Admin);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*супер-адміністратора*");
    }

    [Fact]
    public void UpdateBasicInfo_ShouldUpdateUserInfo()
    {
        // Arrange
        var user = CreateTestUser();
        var newUsername = "newusername";
        var newFirstName = "Jane";
        var newLastName = "Smith";

        // Act
        user.UpdateBasicInfo(newUsername, newFirstName, newLastName);

        // Assert
        user.Username.Should().Be(newUsername);
        user.FirstName.Should().Be(newFirstName);
        user.LastName.Should().Be(newLastName);
        user.FullName.Should().Be("Jane Smith");
        user.LastActivityAt.Should().NotBeNull();
        user.LastActivityAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void UpdateActivity_ShouldUpdateLastActivityTime()
    {
        // Arrange
        var user = CreateTestUser();
        var oldActivity = user.LastActivityAt;

        // Небольша затримка
        Thread.Sleep(100);

        // Act
        user.UpdateActivity();

        // Assert
        user.LastActivityAt.Should().NotBeNull();
        user.LastActivityAt.Should().BeAfter(oldActivity!.Value);
        user.LastActivityAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }
}
