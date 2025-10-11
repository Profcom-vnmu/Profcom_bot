using FluentAssertions;
using StudentUnionBot.Domain.Entities;
using StudentUnionBot.Domain.Enums;
using StudentUnionBot.Tests.Helpers;
using Xunit;

namespace StudentUnionBot.Tests.Domain.Entities;

/// <summary>
/// Тести для сутності Appeal (звернення)
/// </summary>
public class AppealTests : TestBase
{
    [Fact]
    public void Create_WithValidData_ShouldCreateAppeal()
    {
        // Arrange
        var studentId = 123456789L;
        var studentName = "Test Student";
        var category = AppealCategory.Scholarship;
        var subject = "Need help with scholarship";
        var message = "I need information about scholarship application process.";

        // Act
        var appeal = Appeal.Create(studentId, studentName, category, subject, message);

        // Assert
        appeal.Should().NotBeNull();
        appeal.StudentId.Should().Be(studentId);
        appeal.StudentName.Should().Be(studentName);
        appeal.Category.Should().Be(category);
        appeal.Subject.Should().Be(subject);
        appeal.Message.Should().Be(message);
        appeal.Status.Should().Be(AppealStatus.New);
        appeal.Priority.Should().Be(AppealPriority.Normal);
        appeal.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_WithInvalidStudentId_ShouldThrowException(long invalidId)
    {
        // Arrange & Act
        Action act = () => Appeal.Create(
            invalidId, 
            "Test Student", 
            AppealCategory.Scholarship, 
            "Subject", 
            "Message text");

        // Assert
        act.Should().Throw<Exception>();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Create_WithEmptySubject_ShouldThrowException(string? emptySubject)
    {
        // Arrange & Act
        Action act = () => Appeal.Create(
            123456789, 
            "Test Student", 
            AppealCategory.Scholarship, 
            emptySubject!, 
            "Message text");

        // Assert
        act.Should().Throw<Exception>();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Create_WithEmptyMessage_ShouldThrowException(string? emptyMessage)
    {
        // Arrange & Act
        Action act = () => Appeal.Create(
            123456789, 
            "Test Student", 
            AppealCategory.Scholarship, 
            "Subject", 
            emptyMessage!);

        // Assert
        act.Should().Throw<Exception>();
    }

    [Fact]
    public void AssignTo_WithValidAdmin_ShouldAssignAppeal()
    {
        // Arrange
        var appeal = CreateTestAppeal();
        var adminId = 987654321L;

        // Act
        appeal.AssignTo(adminId);

        // Assert
        appeal.AssignedToAdminId.Should().Be(adminId);
        appeal.Status.Should().Be(AppealStatus.InProgress);
        appeal.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void UpdatePriority_WithHighPriority_ShouldUpdatePriority()
    {
        // Arrange
        var appeal = CreateTestAppeal();

        // Act
        appeal.UpdatePriority(AppealPriority.High);

        // Assert
        appeal.Priority.Should().Be(AppealPriority.High);
        appeal.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Close_WithReason_ShouldCloseAppeal()
    {
        // Arrange
        var appeal = CreateTestAppeal();
        var closedBy = 987654321L;
        var reason = "Issue resolved successfully";

        // Act
        appeal.Close(closedBy, reason);

        // Assert
        appeal.Status.Should().Be(AppealStatus.Closed);
        appeal.ClosedReason.Should().Be(reason);
        appeal.ClosedBy.Should().Be(closedBy);
        appeal.ClosedAt.Should().NotBeNull();
        appeal.ClosedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void MarkInProgress_ShouldUpdateStatus()
    {
        // Arrange
        var appeal = CreateTestAppeal();

        // Act
        appeal.MarkInProgress();

        // Assert
        appeal.Status.Should().Be(AppealStatus.InProgress);
        appeal.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Escalate_ShouldUpdateStatusAndPriority()
    {
        // Arrange
        var appeal = CreateTestAppeal();

        // Act
        appeal.Escalate();

        // Assert
        appeal.Status.Should().Be(AppealStatus.Escalated);
        appeal.Priority.Should().Be(AppealPriority.High);
        appeal.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }
}
