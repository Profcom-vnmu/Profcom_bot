# 🧪 StudentUnionBot - Test Suite

**Дата створення**: 11 жовтня 2025  
**Версія**: 1.0  
**Framework**: xUnit 2.6.2

---

## 📊 Поточний стан тестового покриття

### ✅ Реалізовано (60 тестів)

#### Domain Layer
- **AppealTests** (14 тестів) - тестування Appeal entity
  - Factory methods validation
  - Business logic (assign, update priority, close, escalate)
  - Domain rules enforcement
  
- **BotUserTests** (24 тести) - тестування BotUser entity
  - User creation and validation
  - Profile management
  - Email verification workflow
  - Ban/unban functionality
  - Role management
  - Activity tracking

#### Application Layer
- **CreateAppealCommandHandlerTests** (6 тестів) - Handler з мокінгом
  - Success scenarios
  - Rate limiting
  - User validation
  - Banned user handling
  - File attachments
  
- **CreateAppealCommandValidatorTests** (16 тестів) - FluentValidation
  - All field validations
  - Length constraints
  - Required fields
  - Multiple error scenarios

### 📈 Статистика

```
Total Tests: 60
✅ Passed: 60 (100%)
❌ Failed: 0
⏭️ Skipped: 0
⏱️ Duration: ~10s
```

---

## 🏗️ Структура проекту

```
tests/StudentUnionBot.Tests/
├── StudentUnionBot.Tests.csproj    ← NuGet пакети та конфігурація
├── Helpers/
│   └── TestBase.cs                 ← Базовий клас з helper методами
├── Domain/
│   └── Entities/
│       ├── AppealTests.cs
│       └── BotUserTests.cs
└── Application/
    └── Appeals/
        ├── Commands/
        │   └── CreateAppealCommandHandlerTests.cs
        └── Validators/
            └── CreateAppealCommandValidatorTests.cs
```

---

## 🛠️ Технологічний стек

### Testing Frameworks
- **xUnit 2.6.2** - основний фреймворк для unit тестів
- **FluentAssertions 6.12.0** - виразні твердження для читабельності
- **Moq 4.20.70** - мокінг dependencies (IUnitOfWork, Services, Repositories)
- **Microsoft.EntityFrameworkCore.InMemory 8.0.0** - InMemory БД для тестів

### Tools
- **Microsoft.NET.Test.Sdk 17.8.0** - test runner
- **coverlet.collector 6.0.0** - code coverage collection

---

## 🚀 Як запускати тести

### Запуск всіх тестів
```bash
cd tests/StudentUnionBot.Tests
dotnet test
```

### Запуск з детальним виводом
```bash
dotnet test --verbosity normal
```

### Запуск конкретного тестового класу
```bash
dotnet test --filter FullyQualifiedName~AppealTests
```

### Запуск конкретного тесту
```bash
dotnet test --filter "FullyQualifiedName~Create_WithValidData_ShouldCreateAppeal"
```

### Code Coverage
```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

---

## 📝 Приклади тестів

### Domain Entity Test
```csharp
[Fact]
public void Create_WithValidData_ShouldCreateAppeal()
{
    // Arrange
    var studentId = 123456789L;
    var category = AppealCategory.Scholarship;
    
    // Act
    var appeal = Appeal.Create(studentId, "Test Student", category, 
                               "Subject", "Message text");
    
    // Assert
    appeal.Should().NotBeNull();
    appeal.Status.Should().Be(AppealStatus.New);
    appeal.Priority.Should().Be(AppealPriority.Normal);
}
```

### Command Handler Test з Moq
```csharp
[Fact]
public async Task Handle_WithValidCommand_ShouldCreateAppeal()
{
    // Arrange
    var user = CreateTestUser();
    _rateLimiterMock
        .Setup(x => x.AllowAsync(It.IsAny<long>(), It.IsAny<string>(), 
                                 It.IsAny<CancellationToken>()))
        .ReturnsAsync(true);
    
    // Act
    var result = await _handler.Handle(command, CancellationToken.None);
    
    // Assert
    result.IsSuccess.Should().BeTrue();
    _appealRepositoryMock.Verify(
        x => x.AddAsync(It.IsAny<Appeal>(), It.IsAny<CancellationToken>()), 
        Times.Once);
}
```

### Validator Test
```csharp
[Theory]
[InlineData("")]
[InlineData(" ")]
[InlineData(null)]
public void Validate_WithEmptySubject_ShouldFail(string? invalidSubject)
{
    // Arrange
    var command = new CreateAppealCommand { Subject = invalidSubject! };
    
    // Act
    var result = _validator.Validate(command);
    
    // Assert
    result.IsValid.Should().BeFalse();
    result.Errors.Should().Contain(e => e.PropertyName == "Subject");
}
```

---

## 🎯 План розширення тестів

### Пріоритет 1 (Критично)
- [ ] News entity tests
- [ ] Event entity tests
- [ ] News Command Handler tests
- [ ] Event Command Handler tests
- [ ] Query Handler tests (GetActiveAppeals, GetPublishedNews)

### Пріоритет 2 (Важливо)
- [ ] Repository integration tests з InMemory DB
- [ ] Email service tests
- [ ] File service tests
- [ ] Cache service tests
- [ ] Rate limiter tests

### Пріоритет 3 (Бажано)
- [ ] Telegram Bot Handler tests
- [ ] Background Services tests
- [ ] End-to-end integration tests
- [ ] Performance tests

---

## 📐 Best Practices

### 1. Naming Convention
```csharp
// Pattern: MethodName_Scenario_ExpectedResult
[Fact]
public void Create_WithInvalidId_ShouldThrowException() { }
```

### 2. AAA Pattern (Arrange-Act-Assert)
```csharp
// Arrange - setup test data
var user = CreateTestUser();

// Act - execute the code under test
var result = user.Ban("Spam");

// Assert - verify the outcome
user.IsBanned.Should().BeTrue();
```

### 3. Use TestBase для DRY
```csharp
public class MyTests : TestBase
{
    [Fact]
    public void Test()
    {
        var user = CreateTestUser(); // Helper from TestBase
        var appeal = CreateTestAppeal(); // Helper from TestBase
    }
}
```

### 4. Мокінг Dependencies
```csharp
// Setup mock behavior
_repositoryMock
    .Setup(x => x.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
    .ReturnsAsync(entity);

// Verify mock was called
_repositoryMock.Verify(
    x => x.AddAsync(It.IsAny<Entity>(), It.IsAny<CancellationToken>()), 
    Times.Once);
```

### 5. FluentAssertions для читабельності
```csharp
// ✅ Good
result.IsSuccess.Should().BeTrue();
result.Value.Should().NotBeNull();
result.Value!.Id.Should().BeGreaterThan(0);

// ❌ Less readable
Assert.True(result.IsSuccess);
Assert.NotNull(result.Value);
Assert.True(result.Value!.Id > 0);
```

---

## 🐛 Troubleshooting

### Проблема: Тести не знаходяться
```bash
# Перевірте що test SDK встановлений
dotnet list package

# Переконайтесь що .csproj має IsTestProject=true
<IsTestProject>true</IsTestProject>
```

### Проблема: Помилки компіляції з Moq
```csharp
// Переконайтесь що додали using
using Moq;

// Для nullable types використовуйте default()
.ReturnsAsync(default(BotUser));
```

### Проблема: InMemory DB не працює
```csharp
// Додайте using
using Microsoft.EntityFrameworkCore;

// Кожен тест має унікальну БД
.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
```

---

## 📚 Корисні ресурси

- [xUnit Documentation](https://xunit.net/)
- [FluentAssertions Documentation](https://fluentassertions.com/)
- [Moq Documentation](https://github.com/moq/moq4)
- [EF Core InMemory Provider](https://learn.microsoft.com/en-us/ef/core/providers/in-memory/)

---

## 🤝 Contribution Guidelines

При додаванні нових тестів:
1. Дотримуйтесь існуючої структури папок
2. Успадковуйтесь від `TestBase` коли потрібні helper методи
3. Використовуйте AAA pattern
4. Називайте тести описово (`MethodName_Scenario_ExpectedResult`)
5. Додавайте summary коментарі до тестових класів
6. Групуйте пов'язані тести в одному файлі
7. Перевіряйте що всі тести проходять перед commit

---

**Last Updated**: 11 жовтня 2025  
**Maintainer**: Development Team  
**Status**: ✅ Active Development
