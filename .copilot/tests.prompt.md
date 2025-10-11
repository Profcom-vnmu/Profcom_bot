# Unit Tests Development Prompt

**Мета:** Інструкції для написання unit tests у проєкті StudentUnionBot з використанням xUnit, FluentAssertions та Moq.

---

## Коли писати тести?

✅ **ЗАВЖДИ пиши тести для:**
- Domain entity factory methods (Appeal.Create, News.Create)
- Command Handlers (CreateAppeal, UpdateNews, etc.)
- Query Handlers (GetAppeals, GetNews, etc.)
- Validators (FluentValidation)
- Services з бізнес-логікою (AppealAssignmentService, AuthorizationService)
- Extension методи для enums

❌ **НЕ потрібні тести для:**
- DTOs (прості data transfer objects)
- Simple POCO classes
- Configuration classes
- Auto-generated EF migrations

---

## Базовий шаблон тесту

```csharp
using Xunit;
using FluentAssertions;
using Moq;
using StudentUnionBot.Application.{Feature}.Commands.{Action};
using StudentUnionBot.Domain.Entities;
using StudentUnionBot.Domain.Interfaces;

namespace StudentUnionBot.Tests.Application.{Feature};

public class {Action}CommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ILogger<{Action}CommandHandler>> _loggerMock;
    private readonly {Action}CommandHandler _handler;

    public {Action}CommandHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<{Action}CommandHandler>>();
        
        _handler = new {Action}CommandHandler(
            _unitOfWorkMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldReturnSuccess()
    {
        // Arrange
        var command = new {Action}Command(/* parameters */);
        
        // Setup mocks
        _unitOfWorkMock
            .Setup(x => x.{Repository}.{Method}(It.IsAny<{Type}>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(/* expected result */);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        
        // Verify method was called
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_InvalidData_ShouldReturnFailure()
    {
        // Arrange
        var command = new {Action}Command(/* invalid data */);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNullOrEmpty();
    }
}
```

---

## Naming Conventions для тестів

### Метод тестування
```csharp
// Pattern: MethodName_Scenario_ExpectedResult

[Fact]
public void Create_ValidData_ShouldCreateAppeal()

[Fact]
public void Create_EmptySubject_ShouldThrowDomainException()

[Fact]
public async Task Handle_UserNotFound_ShouldReturnFailure()

[Fact]
public async Task Handle_RateLimitExceeded_ShouldReturnFailure()
```

### Клас тестів
```csharp
// Pattern: {ClassUnderTest}Tests

public class AppealTests { }
public class CreateAppealCommandHandlerTests { }
public class GetAppealsQueryHandlerTests { }
public class CreateAppealCommandValidatorTests { }
```

---

## 1. Тести для Domain Entities

### Приклад: Appeal Entity

```csharp
public class AppealTests
{
    [Fact]
    public void Create_ValidData_ShouldCreateAppeal()
    {
        // Arrange
        var studentId = 123L;
        var studentName = "Test Student";
        var category = AppealCategory.Scholarship;
        var subject = "Valid Subject";
        var message = "Valid message with more than 10 characters";

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
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_EmptySubject_ShouldThrowDomainException(string subject)
    {
        // Arrange
        var studentId = 123L;
        var studentName = "Test Student";
        var category = AppealCategory.Scholarship;
        var message = "Valid message";

        // Act & Assert
        var exception = Assert.Throws<DomainException>(() => 
            Appeal.Create(studentId, studentName, category, subject, message));
        
        exception.Message.Should().Contain("Subject");
    }

    [Fact]
    public void Create_MessageTooShort_ShouldThrowDomainException()
    {
        // Arrange
        var message = "Short"; // Less than 10 characters

        // Act & Assert
        var exception = Assert.Throws<DomainException>(() => 
            Appeal.Create(123, "Student", AppealCategory.Scholarship, "Subject", message));
        
        exception.Message.Should().Contain("Message too short");
    }

    [Fact]
    public void AssignTo_ValidAdminId_ShouldAssignAppeal()
    {
        // Arrange
        var appeal = Appeal.Create(123, "Student", AppealCategory.Scholarship, "Subject", "Message");
        var adminId = 456L;

        // Act
        appeal.AssignTo(adminId);

        // Assert
        appeal.AssignedToAdminId.Should().Be(adminId);
        appeal.Status.Should().Be(AppealStatus.Assigned);
        appeal.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void AssignTo_ClosedAppeal_ShouldThrowDomainException()
    {
        // Arrange
        var appeal = Appeal.Create(123, "Student", AppealCategory.Scholarship, "Subject", "Message");
        appeal.Close(456, "Resolved");

        // Act & Assert
        var exception = Assert.Throws<DomainException>(() => appeal.AssignTo(789));
        exception.Message.Should().Contain("closed");
    }

    [Fact]
    public void Close_ValidReason_ShouldCloseAppeal()
    {
        // Arrange
        var appeal = Appeal.Create(123, "Student", AppealCategory.Scholarship, "Subject", "Message");
        var adminId = 456L;
        var reason = "Issue resolved successfully";

        // Act
        appeal.Close(adminId, reason);

        // Assert
        appeal.Status.Should().Be(AppealStatus.Closed);
        appeal.ClosedAt.Should().NotBeNull();
        appeal.ClosedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        appeal.ClosedReason.Should().Be(reason);
    }

    [Fact]
    public void Close_AlreadyClosed_ShouldThrowDomainException()
    {
        // Arrange
        var appeal = Appeal.Create(123, "Student", AppealCategory.Scholarship, "Subject", "Message");
        appeal.Close(456, "First close");

        // Act & Assert
        var exception = Assert.Throws<DomainException>(() => appeal.Close(456, "Second close"));
        exception.Message.Should().Contain("already closed");
    }

    [Fact]
    public void SetPriority_ValidPriority_ShouldUpdatePriority()
    {
        // Arrange
        var appeal = Appeal.Create(123, "Student", AppealCategory.Scholarship, "Subject", "Message");

        // Act
        appeal.SetPriority(AppealPriority.Urgent);

        // Assert
        appeal.Priority.Should().Be(AppealPriority.Urgent);
    }
}
```

---

## 2. Тести для Command Handlers

### Приклад: CreateAppealCommandHandler

```csharp
public class CreateAppealCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IRateLimiter> _rateLimiterMock;
    private readonly Mock<INotificationService> _notificationServiceMock;
    private readonly Mock<IAppealAssignmentService> _assignmentServiceMock;
    private readonly Mock<ILogger<CreateAppealCommandHandler>> _loggerMock;
    private readonly CreateAppealCommandHandler _handler;

    public CreateAppealCommandHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _rateLimiterMock = new Mock<IRateLimiter>();
        _notificationServiceMock = new Mock<INotificationService>();
        _assignmentServiceMock = new Mock<IAppealAssignmentService>();
        _loggerMock = new Mock<ILogger<CreateAppealCommandHandler>>();
        
        _handler = new CreateAppealCommandHandler(
            _unitOfWorkMock.Object,
            _rateLimiterMock.Object,
            _notificationServiceMock.Object,
            _assignmentServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldReturnSuccess()
    {
        // Arrange
        var command = new CreateAppealCommand(
            StudentId: 123,
            StudentName: "Test User",
            Category: AppealCategory.Scholarship,
            Subject: "Test Subject",
            Message: "Test message with more than 10 characters",
            PhotoFileId: null,
            DocumentFileId: null,
            DocumentFileName: null);

        var user = new BotUser 
        { 
            TelegramId = 123, 
            IsBanned = false,
            FullName = "Test User"
        };
        
        // Setup mocks
        _rateLimiterMock
            .Setup(x => x.AllowAsync(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _unitOfWorkMock
            .Setup(x => x.Users.GetByTelegramIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _unitOfWorkMock
            .Setup(x => x.Appeals.AddAsync(It.IsAny<Appeal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Appeal a, CancellationToken ct) => a);

        _notificationServiceMock
            .Setup(x => x.NotifyAllAdminsAsync(
                It.IsAny<NotificationEvent>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<NotificationPriority>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<int>.Ok(3)); // 3 admins notified

        _assignmentServiceMock
            .Setup(x => x.AssignAppealAsync(It.IsAny<Appeal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<BotUser>.Ok(new BotUser { TelegramId = 456 }));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Subject.Should().Be("Test Subject");
        result.Value.Status.Should().Be(AppealStatus.New);
        
        // Verify interactions
        _unitOfWorkMock.Verify(
            x => x.Appeals.AddAsync(It.IsAny<Appeal>(), It.IsAny<CancellationToken>()),
            Times.Once);
        
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);

        _notificationServiceMock.Verify(
            x => x.NotifyAllAdminsAsync(
                NotificationEvent.AppealCreated,
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<NotificationPriority>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_RateLimitExceeded_ShouldReturnFailure()
    {
        // Arrange
        var command = new CreateAppealCommand(123, "User", AppealCategory.Scholarship, 
            "Subject", "Message", null, null, null);

        _rateLimiterMock
            .Setup(x => x.AllowAsync(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _rateLimiterMock
            .Setup(x => x.GetTimeUntilResetAsync(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TimeSpan.FromMinutes(10));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("ліміт");
        
        // Verify Appeal was NOT created
        _unitOfWorkMock.Verify(
            x => x.Appeals.AddAsync(It.IsAny<Appeal>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_UserNotFound_ShouldReturnFailure()
    {
        // Arrange
        var command = new CreateAppealCommand(123, "User", AppealCategory.Scholarship, 
            "Subject", "Message", null, null, null);

        _rateLimiterMock
            .Setup(x => x.AllowAsync(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _unitOfWorkMock
            .Setup(x => x.Users.GetByTelegramIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((BotUser?)null); // User not found

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("не знайдений");
    }

    [Fact]
    public async Task Handle_BannedUser_ShouldReturnFailure()
    {
        // Arrange
        var command = new CreateAppealCommand(123, "User", AppealCategory.Scholarship, 
            "Subject", "Message", null, null, null);

        var bannedUser = new BotUser 
        { 
            TelegramId = 123, 
            IsBanned = true,
            BanReason = "Spam"
        };

        _rateLimiterMock
            .Setup(x => x.AllowAsync(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _unitOfWorkMock
            .Setup(x => x.Users.GetByTelegramIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(bannedUser);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("заблоковано");
        result.Error.Should().Contain("Spam");
    }

    [Fact]
    public async Task Handle_WithPhotoFile_ShouldCreateAppealWithMessage()
    {
        // Arrange
        var command = new CreateAppealCommand(
            StudentId: 123,
            StudentName: "User",
            Category: AppealCategory.Scholarship,
            Subject: "Subject",
            Message: "Message with photo",
            PhotoFileId: "photo_123",
            DocumentFileId: null,
            DocumentFileName: null);

        var user = new BotUser { TelegramId = 123, IsBanned = false };

        _rateLimiterMock
            .Setup(x => x.AllowAsync(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _unitOfWorkMock
            .Setup(x => x.Users.GetByTelegramIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _unitOfWorkMock
            .Setup(x => x.Appeals.AddAsync(It.IsAny<Appeal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Appeal a, CancellationToken ct) => a);

        _notificationServiceMock
            .Setup(x => x.NotifyAllAdminsAsync(
                It.IsAny<NotificationEvent>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<NotificationPriority>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<int>.Ok(3));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        
        // Verify Appeal was saved twice (initial + with message)
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.AtLeast(2));
    }
}
```

---

## 3. Тести для Query Handlers

### Приклад: GetUserAppealsQueryHandler

```csharp
public class GetUserAppealsQueryHandlerTests : IDisposable
{
    private readonly BotDbContext _context;
    private readonly AppealRepository _repository;
    private readonly GetUserAppealsQueryHandler _handler;

    public GetUserAppealsQueryHandlerTests()
    {
        // Create In-Memory database
        var options = new DbContextOptionsBuilder<BotDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new BotDbContext(options);
        _repository = new AppealRepository(_context);
        
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(x => x.Appeals).Returns(_repository);

        _handler = new GetUserAppealsQueryHandler(
            unitOfWork.Object,
            Mock.Of<ILogger<GetUserAppealsQueryHandler>>());
    }

    [Fact]
    public async Task Handle_UserHasAppeals_ShouldReturnAppeals()
    {
        // Arrange
        var userId = 123L;
        var user = new BotUser { TelegramId = userId, FullName = "Test User" };
        _context.Users.Add(user);

        var appeal1 = Appeal.Create(userId, "Test User", AppealCategory.Scholarship, 
            "Subject 1", "Message 1");
        var appeal2 = Appeal.Create(userId, "Test User", AppealCategory.Dormitory, 
            "Subject 2", "Message 2");
        
        await _repository.AddAsync(appeal1);
        await _repository.AddAsync(appeal2);
        await _context.SaveChangesAsync();

        var query = new GetUserAppealsQuery(userId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Should().Contain(a => a.Subject == "Subject 1");
        result.Value.Should().Contain(a => a.Subject == "Subject 2");
        result.Value.Should().AllSatisfy(a => a.StudentId.Should().Be(userId));
    }

    [Fact]
    public async Task Handle_UserHasNoAppeals_ShouldReturnEmptyList()
    {
        // Arrange
        var userId = 123L;
        var user = new BotUser { TelegramId = userId };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var query = new GetUserAppealsQuery(userId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_MultipleUsers_ShouldReturnOnlyUserAppeals()
    {
        // Arrange
        var userId1 = 123L;
        var userId2 = 456L;
        
        var user1 = new BotUser { TelegramId = userId1 };
        var user2 = new BotUser { TelegramId = userId2 };
        _context.Users.AddRange(user1, user2);

        var appeal1 = Appeal.Create(userId1, "User1", AppealCategory.Scholarship, "Subject 1", "Message 1");
        var appeal2 = Appeal.Create(userId2, "User2", AppealCategory.Scholarship, "Subject 2", "Message 2");
        
        await _repository.AddAsync(appeal1);
        await _repository.AddAsync(appeal2);
        await _context.SaveChangesAsync();

        var query = new GetUserAppealsQuery(userId1);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value.First().StudentId.Should().Be(userId1);
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}
```

---

## 4. Тести для Validators

### Приклад: CreateAppealCommandValidator

```csharp
public class CreateAppealCommandValidatorTests
{
    private readonly CreateAppealCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_ShouldPass()
    {
        // Arrange
        var command = new CreateAppealCommand(
            StudentId: 123,
            StudentName: "Test User",
            Category: AppealCategory.Scholarship,
            Subject: "Valid Subject",
            Message: "Valid message with more than 10 characters",
            PhotoFileId: null,
            DocumentFileId: null,
            DocumentFileName: null);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_EmptySubject_ShouldFail(string subject)
    {
        // Arrange
        var command = new CreateAppealCommand(123, "User", AppealCategory.Scholarship, 
            subject, "Valid message", null, null, null);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateAppealCommand.Subject));
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("обов'язков"));
    }

    [Theory]
    [InlineData("AB")]     // 2 chars
    [InlineData("ABC")]    // 3 chars
    [InlineData("ABCD")]   // 4 chars
    public void Validate_SubjectTooShort_ShouldFail(string subject)
    {
        // Arrange
        var command = new CreateAppealCommand(123, "User", AppealCategory.Scholarship, 
            subject, "Valid message", null, null, null);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => 
            e.PropertyName == nameof(CreateAppealCommand.Subject) &&
            e.ErrorMessage.Contains("5"));
    }

    [Fact]
    public void Validate_SubjectTooLong_ShouldFail()
    {
        // Arrange
        var subject = new string('A', 201); // 201 characters
        var command = new CreateAppealCommand(123, "User", AppealCategory.Scholarship, 
            subject, "Valid message", null, null, null);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => 
            e.PropertyName == nameof(CreateAppealCommand.Subject) &&
            e.ErrorMessage.Contains("200"));
    }

    [Theory]
    [InlineData("Short")]  // 5 chars
    [InlineData("Message")] // 7 chars
    public void Validate_MessageTooShort_ShouldFail(string message)
    {
        // Arrange
        var command = new CreateAppealCommand(123, "User", AppealCategory.Scholarship, 
            "Subject", message, null, null, null);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => 
            e.PropertyName == nameof(CreateAppealCommand.Message) &&
            e.ErrorMessage.Contains("10"));
    }

    [Fact]
    public void Validate_MessageTooLong_ShouldFail()
    {
        // Arrange
        var message = new string('A', 4001); // 4001 characters
        var command = new CreateAppealCommand(123, "User", AppealCategory.Scholarship, 
            "Subject", message, null, null, null);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => 
            e.PropertyName == nameof(CreateAppealCommand.Message) &&
            e.ErrorMessage.Contains("4000"));
    }

    [Fact]
    public void Validate_InvalidStudentId_ShouldFail()
    {
        // Arrange
        var command = new CreateAppealCommand(0, "User", AppealCategory.Scholarship, 
            "Subject", "Valid message", null, null, null);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateAppealCommand.StudentId));
    }

    [Fact]
    public void Validate_InvalidCategory_ShouldFail()
    {
        // Arrange
        var command = new CreateAppealCommand(123, "User", (AppealCategory)999, 
            "Subject", "Valid message", null, null, null);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateAppealCommand.Category));
    }
}
```

---

## 5. Тести для Services

### Приклад: AppealAssignmentService

```csharp
public class AppealAssignmentServiceTests : IDisposable
{
    private readonly BotDbContext _context;
    private readonly AppealAssignmentService _service;

    public AppealAssignmentServiceTests()
    {
        var options = new DbContextOptionsBuilder<BotDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new BotDbContext(options);
        
        var unitOfWork = new UnitOfWork(_context, Mock.Of<ILogger<UserRepository>>());
        var logger = Mock.Of<ILogger<AppealAssignmentService>>();

        _service = new AppealAssignmentService(unitOfWork, logger);
    }

    [Fact]
    public async Task AssignAppealAsync_ShouldAssignToLeastBusyAdmin()
    {
        // Arrange
        var admin1 = new BotUser { TelegramId = 1, Role = UserRole.Admin, FullName = "Admin 1" };
        var admin2 = new BotUser { TelegramId = 2, Role = UserRole.Admin, FullName = "Admin 2" };
        var admin3 = new BotUser { TelegramId = 3, Role = UserRole.Admin, FullName = "Admin 3" };
        
        _context.Users.AddRange(admin1, admin2, admin3);

        var workload1 = new AdminWorkload { AdminId = 1, ActiveAppealsCount = 5, IsAvailable = true };
        var workload2 = new AdminWorkload { AdminId = 2, ActiveAppealsCount = 2, IsAvailable = true }; // Least busy
        var workload3 = new AdminWorkload { AdminId = 3, ActiveAppealsCount = 8, IsAvailable = true };
        
        _context.AdminWorkloads.AddRange(workload1, workload2, workload3);
        await _context.SaveChangesAsync();

        var appeal = Appeal.Create(123, "User", AppealCategory.Scholarship, "Subject", "Message");

        // Act
        var result = await _service.AssignAppealAsync(appeal, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.TelegramId.Should().Be(2); // Should assign to admin2 (least busy)
        appeal.AssignedToAdminId.Should().Be(2);
        appeal.Status.Should().Be(AppealStatus.Assigned);
    }

    [Fact]
    public async Task AssignAppealAsync_WithCategoryExpertise_ShouldPrioritizeExpert()
    {
        // Arrange
        var admin1 = new BotUser { TelegramId = 1, Role = UserRole.Admin };
        var admin2 = new BotUser { TelegramId = 2, Role = UserRole.Admin }; // Expert in Scholarship

        _context.Users.AddRange(admin1, admin2);

        var workload1 = new AdminWorkload { AdminId = 1, ActiveAppealsCount = 2, IsAvailable = true };
        var workload2 = new AdminWorkload { AdminId = 2, ActiveAppealsCount = 5, IsAvailable = true };

        var expertise = new AdminCategoryExpertise 
        { 
            AdminId = 2, 
            Category = AppealCategory.Scholarship,
            ExperienceLevel = 5,
            SuccessfulResolutions = 100
        };

        _context.AdminWorkloads.AddRange(workload1, workload2);
        _context.AdminCategoryExpertises.Add(expertise);
        await _context.SaveChangesAsync();

        var appeal = Appeal.Create(123, "User", AppealCategory.Scholarship, "Subject", "Message");

        // Act
        var result = await _service.AssignAppealAsync(appeal, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TelegramId.Should().Be(2); // Should assign to expert even if busier
    }

    [Fact]
    public async Task AssignAppealAsync_NoAvailableAdmins_ShouldReturnFailure()
    {
        // Arrange
        var admin1 = new BotUser { TelegramId = 1, Role = UserRole.Admin };
        _context.Users.Add(admin1);

        var workload = new AdminWorkload { AdminId = 1, ActiveAppealsCount = 5, IsAvailable = false }; // Not available
        _context.AdminWorkloads.Add(workload);
        await _context.SaveChangesAsync();

        var appeal = Appeal.Create(123, "User", AppealCategory.Scholarship, "Subject", "Message");

        // Act
        var result = await _service.AssignAppealAsync(appeal, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Немає доступних");
        appeal.AssignedToAdminId.Should().BeNull();
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}
```

---

## 6. Test Helpers та Utilities

### TestBase клас

```csharp
public abstract class TestBase : IDisposable
{
    protected BotDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<BotDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .EnableSensitiveDataLogging()
            .Options;

        return new BotDbContext(options);
    }

    protected Mock<ILogger<T>> CreateLoggerMock<T>()
    {
        return new Mock<ILogger<T>>();
    }

    protected BotUser CreateTestUser(
        long telegramId = 123,
        string fullName = "Test User",
        UserRole role = UserRole.Student,
        bool isBanned = false)
    {
        return new BotUser
        {
            TelegramId = telegramId,
            FullName = fullName,
            Role = role,
            IsBanned = isBanned,
            Language = Language.Ukrainian,
            RegistrationDate = DateTime.UtcNow
        };
    }

    protected Appeal CreateTestAppeal(
        long studentId = 123,
        string studentName = "Test User",
        AppealCategory category = AppealCategory.Scholarship)
    {
        return Appeal.Create(
            studentId: studentId,
            studentName: studentName,
            category: category,
            subject: "Test Subject",
            message: "Test message with more than 10 characters");
    }

    public virtual void Dispose()
    {
        // Cleanup if needed
    }
}
```

---

## Best Practices для тестів

### 1. Arrange-Act-Assert Pattern
```csharp
[Fact]
public async Task TestName()
{
    // Arrange - Setup test data and mocks
    var command = new SomeCommand();
    _mockService.Setup(...);

    // Act - Execute the operation
    var result = await _handler.Handle(command);

    // Assert - Verify results
    result.Should().BeTrue();
}
```

### 2. Один Assert на концепцію
```csharp
// ✅ ДОБРЕ - перевіряємо одну концепцію
[Fact]
public void Create_ValidData_ShouldSetProperties()
{
    var appeal = Appeal.Create(...);
    appeal.Status.Should().Be(AppealStatus.New);
}

[Fact]
public void Create_ValidData_ShouldSetCreatedAt()
{
    var appeal = Appeal.Create(...);
    appeal.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
}

// ❌ ПОГАНО - багато перевірок в одному тесті
[Fact]
public void Create_ValidData_ShouldWorkCorrectly()
{
    var appeal = Appeal.Create(...);
    appeal.Status.Should().Be(AppealStatus.New);
    appeal.Priority.Should().Be(AppealPriority.Normal);
    appeal.CreatedAt.Should().NotBeNull();
    // ... 10 more assertions
}
```

### 3. Використовуй Theory для параметризованих тестів
```csharp
[Theory]
[InlineData("")]
[InlineData("   ")]
[InlineData(null)]
public void Validate_InvalidSubject_ShouldFail(string subject)
{
    // Test implementation
}
```

### 4. Mock тільки зовнішні залежності
```csharp
// ✅ ДОБРЕ - Mock external services
var emailServiceMock = new Mock<IEmailService>();
var repositoryMock = new Mock<IAppealRepository>();

// ❌ ПОГАНО - Don't mock value objects or DTOs
var dtoMock = new Mock<AppealDto>(); // NO!
```

### 5. Cleanup після тестів
```csharp
public class MyTests : IDisposable
{
    private readonly BotDbContext _context;

    public MyTests()
    {
        _context = CreateInMemoryContext();
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}
```

---

## Code Coverage Target

**Мінімум:** 80% coverage для:
- Domain entities
- Command/Query Handlers
- Validators
- Critical services

**Виключення з coverage:**
- DTOs
- Migrations
- Program.cs
- Configuration classes

---

## Running Tests

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test
dotnet test --filter "FullyQualifiedName~CreateAppealCommandHandlerTests"

# Run tests in watch mode
dotnet watch test
```

---

**Створено:** 11 жовтня 2025  
**Автор:** GitHub Copilot AI Agent  
**Призначення:** Інструкції для написання якісних unit tests
