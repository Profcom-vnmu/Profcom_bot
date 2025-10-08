# 📋 Опис проекту "Бот Студентського профкому" (Оновлена версія)

## 🎯 Призначення

Telegram-бот для студентського профкому університету з використанням сучасних технологій та best practices:
- Комунікація між студентами та профкомом
- Обробка звернень студентів з категоризацією
- Розсилка новин та оголошень
- Інформаційна підтримка студентів
- Система верифікації та безпеки

---

## 🏗️ Технологічний стек (Оновлений)

### Backend
- **.NET 8.0** - основний фреймворк з Minimal API
- **C# 12** - мова програмування
- **Telegram.Bot 19.0.0** - взаємодія з Telegram Bot API
- **Entity Framework Core 8.0** - ORM для роботи з БД
- **MediatR** - CQRS pattern для business logic
- **FluentValidation** - валідація вхідних даних

### База даних
- **SQLite** - для локальної розробки (Development)
- **PostgreSQL** - для production (Render.com)
- **Redis** - кешування користувацьких сесій та rate limiting

### Логування та моніторинг
- **Serilog** - структуроване логування
- **Serilog.Sinks.File** - логування в файли з ротацією
- **Serilog.Sinks.Console** - логування в консоль
- **Prometheus.NET** - метрики та моніторинг
- **Health Checks** - перевірка стану системи

### Безпека
- **AspNetCore.RateLimiting** - захист від спаму
- **Email Verification** - верифікація студентів
- **Data Protection** - шифрування даних

### Додатково
- **Npgsql** - драйвер PostgreSQL
- **StackExchange.Redis** - клієнт для Redis
- **Polly** - retry policies та resilience
- **AutoMapper** - mapping між об'єктами

---

## 📊 Поточний функціонал (Покращений)

### Для студентів 👨‍🎓

1. **Верифікація та реєстрація**
   - Email верифікація через університетську пошту
   - Автоматичне заповнення профілю (факультет, курс, група)
   - Статус "Верифікований студент"
   - Можливість оновити дані профілю

2. **Звернення з категоріями**
   - 🏠 Гуртожиток
   - 📢 Пропозиція
   - ⚠️ Скарга
   - ❓ Інше
   
   **Функції:**
   - Створення звернення з вибором категорії
   - Перегляд історії своїх звернень
   - Додавання повідомлень до існуючого звернення
   - Закриття звернення 
   - Підтримка медіа (фото, документи)
   - Пошук по власних зверненнях

3. **Інформаційні розділи**
   - Довідка (інтерактивна допомога)
   - Контактна інформація (з кнопками зв'язку)
   - Інформація про гуртожиток
   - Можливості для студентів
   - Партнери та знижки (з QR-кодами)
   - Календар заходів
   - Корисні посилання (електронний журнал, CampusHub, розклад)

4. **Новини та оголошення**
   - Отримання розсилок з категоризацією
   - Налаштування категорій новин (що отримувати)
   - Сповіщення про нові відповіді на звернення
   - Важливі оголошення (пін-повідомлення)

5. **Налаштування**
   - Вибір мови інтерфейсу (🇺🇦 Українська / 🇬🇧 Англійська)
   - Налаштування сповіщень по категоріях
   - Часовий пояс

### Для адміністраторів 👨‍💼

1. **Управління зверненнями**
   - Перегляд активних звернень з фільтрацією по категоріях
   - Перегляд закритих звернень
   - Пошук звернень (по ID, тексту, студенту, даті)
   - Відповідь на звернення з шаблонами
   - Закриття звернень
   - Переадресація звернення іншому адміну
   - Пагінація з індикатором прогресу (📋 1-5 з 23 | Сторінка 1/5)
   - Індикація непрочитаних повідомлень
   - Ескалація (автоматичне нагадування через 24/48 годин)

2. **Публікація новин**
   - Створення оголошень з категоріями
   - Підтримка фото, відео, галерей
   - Запланована розсилка (вибір дати та часу)
   - Таргетована розсилка (по курсах, факультетах)
   - Масова розсилка всім активним користувачам
   - Перегляд статистики переглядів
   - Редагування/скасування запланованих оголошень

3. **Редагування контенту**
   - Контактна інформація
   - Інформація про партнерів (з можливістю додавання QR-кодів)
   - Інформація про заходи
   - Шаблони відповідей на звернення
   - FAQ розділ

4. **Статистика та аналітика**
   - Загальна статистика користувачів
   - Кількість активних/закритих звернень
   - Розподіл за курсами/факультетами
   - Розподіл звернень по категоріях
   - Середній час відповіді на звернення
   - Найактивніші години/дні
   - Експорт статистики (PDF, Excel, CSV)
   - Графіки та діаграми

5. **Система ролей та прав**
   - Супер-адміністратор (повний доступ)
   - Адміністратор (управління зверненнями та новинами)
   - Логування всіх дій адмінів

---

## 🗄️ Моделі даних (Оновлені)

### BotUser (Користувач)
```csharp
- TelegramId (long, PK) - ID в Telegram
- Username (string?) - @username
- FirstName (string?) - Ім'я
- LastName (string?) - Прізвище
- FullName (string?) - ПІБ повністю
- Faculty (string?) - Факультет
- Course (int?) - Курс (1-6)
- Group (string?) - Група
- Email (string?) - Університетська пошта
- IsEmailVerified (bool) - Email підтверджено
- VerificationCode (string?) - Код верифікації
- VerificationCodeExpiry (DateTime?) - Термін дії коду
- Language (string) - Мова інтерфейсу (uk/en)
- TimeZone (string?) - Часовий пояс
- NotificationSettings (string?) - JSON з налаштуваннями
- JoinedAt (DateTime) - Дата реєстрації
- IsActive (bool) - Активний користувач
- IsBanned (bool) - Заблокований
- BanReason (string?) - Причина блокування
- ProfileUpdatedAt (DateTime?) - Останнє оновлення профілю
- LastActivityAt (DateTime?) - Остання активність
- Role (UserRole) - Роль користувача
```

### UserRole (Enum)
```csharp
- Student - Студент
- Admin - Адміністратор
- SuperAdmin - Супер-адміністратор
```

### Appeal (Звернення)
```csharp
- Id (int, PK) - Ідентифікатор
- StudentId (long, FK) - ID студента
- StudentName (string) - Ім'я студента
- Category (AppealCategory) - Категорія звернення
- Subject (string) - Тема звернення
- Message (string) - Текст звернення
- Status (AppealStatus) - Статус
- Priority (AppealPriority) - Пріоритет
- AssignedToAdminId (long?) - Призначено адміну
- CreatedAt (DateTime) - Дата створення
- UpdatedAt (DateTime) - Дата оновлення
- FirstResponseAt (DateTime?) - Час першої відповіді
- ClosedAt (DateTime?) - Дата закриття
- ClosedBy (long?) - Хто закрив
- ClosedReason (string?) - Причина закриття
- Rating (int?) - Оцінка студента (1-5)
- RatingComment (string?) - Коментар до оцінки
- Messages (ICollection<AppealMessage>) - Повідомлення
- Student (BotUser) - Navigation property
```

### AppealCategory (Enum)
```csharp
- Events - Заходи
- Proposal - Пропозиція
- Complaint - Скарга
- Other - Інше
```

### AppealStatus (Enum)
```csharp
- New - Нове звернення
- InProgress - В роботі
- WaitingForStudent - Очікує відповіді студента
- WaitingForAdmin - Очікує відповіді адміна
- Escalated - Ескальовано
- Resolved - Вирішено
- Closed - Закрито
```

### AppealPriority (Enum)
```csharp
- Low - Низький
- Normal - Нормальний
- High - Високий
- Urgent - Терміновий
```

### AppealMessage (Повідомлення звернення)
```csharp
- Id (int, PK) - Ідентифікатор
- AppealId (int, FK) - ID звернення
- SenderId (long) - ID відправника
- SenderName (string) - Ім'я відправника
- IsFromAdmin (bool) - Від адміністратора
- Text (string) - Текст повідомлення
- SentAt (DateTime) - Час відправки
- IsRead (bool) - Прочитано
- ReadAt (DateTime?) - Час прочитання
- PhotoFileId (string?) - ID фото в Telegram
- DocumentFileId (string?) - ID документа в Telegram
- DocumentFileName (string?) - Назва файлу
- VideoUrl (string?) - Посилання на відео
- IsTemplate (bool) - Шаблонна відповідь
- TemplateId (int?) - ID шаблону
- Appeal (Appeal) - Navigation property
```

### News (Новина)
```csharp
- Id (int, PK) - Ідентифікатор
- Title (string) - Заголовок
- Content (string) - Зміст
- Category (NewsCategory) - Категорія
- Priority (NewsPriority) - Пріоритет
- CreatedAt (DateTime) - Дата створення
- PublishAt (DateTime?) - Дата публікації
- IsPublished (bool) - Опубліковано
- IsPinned (bool) - Закріплено
- PhotoFileIds (string?) - JSON масив ID фото
- VideoUrl (string?) - Посилання на відео
- TargetCourses (string?) - JSON масив курсів
- TargetFaculties (string?) - JSON масив факультетів
- ViewCount (int) - Кількість переглядів
- CreatedBy (long) - Хто створив
- UpdatedAt (DateTime?) - Дата оновлення
```

### NewsCategory (Enum)
```csharp
- Important - Важливо
- Education - Освітні
- Cultural - Культурні
- Sport - Спортивні
- Administrative - Адміністративні
- Events - Заходи
```

### NewsPriority (Enum)
```csharp
- Normal - Звичайна
- High - Важлива
- Urgent - Термінова
```

### MessageTemplate (Шаблон відповіді)
```csharp
- Id (int, PK) - Ідентифікатор
- Title (string) - Назва шаблону
- Content (string) - Зміст шаблону
- Category (AppealCategory?) - Категорія (опціонально)
- CreatedBy (long) - Хто створив
- CreatedAt (DateTime) - Дата створення
- UsageCount (int) - Кількість використань
- IsActive (bool) - Активний
```

### ContactInfo (Контакти)
```csharp
- Id (int, PK) - Ідентифікатор
- Type (ContactType) - Тип контакту
- Title (string) - Заголовок
- Content (string) - Вміст
- PhoneNumber (string?) - Телефон
- Email (string?) - Email
- TelegramLink (string?) - Telegram
- WorkingHours (string?) - Години роботи
- UpdatedAt (DateTime) - Дата оновлення
- UpdatedBy (long?) - Хто оновив
```

### ContactType (Enum)
```csharp
- StudentUnion - Профспілка
- Deanery - Деканат
- Library - Бібліотека
- Dormitory - Гуртожиток
- Other - Інше
```

### Event (Захід)
```csharp
- Id (int, PK) - Ідентифікатор
- Title (string) - Назва
- Description (string) - Опис
- Category (EventCategory) - Категорія
- StartDate (DateTime) - Дата початку
- EndDate (DateTime?) - Дата закінчення
- Location (string?) - Місце проведення
- MaxParticipants (int?) - Макс. учасників
- PhotoFileId (string?) - ID фото
- IsActive (bool) - Активний
- CreatedAt (DateTime) - Дата створення
- CreatedBy (long) - Хто створив
- Participants (ICollection<EventParticipant>) - Учасники
```

### EventCategory (Enum)
```csharp
- Cultural - Культурний
- Sport - Спортивний
- Educational - Освітній
- Social - Соціальний
- Other - Інше
```

### EventParticipant (Учасник заходу)
```csharp
- Id (int, PK) - Ідентифікатор
- EventId (int, FK) - ID заходу
- UserId (long, FK) - ID користувача
- RegisteredAt (DateTime) - Дата реєстрації
- Status (ParticipantStatus) - Статус
- Event (Event) - Navigation property
- User (BotUser) - Navigation property
```

### ParticipantStatus (Enum)
```csharp
- Registered - Зареєстрований
- Confirmed - Підтверджено
- Attended - Відвідав
- Cancelled - Скасовано
```

### AdminLog (Лог дій адміністратора)
```csharp
- Id (int, PK) - Ідентифікатор
- AdminId (long) - ID адміністратора
- AdminName (string) - Ім'я адміністратора
- Action (AdminAction) - Дія
- EntityType (string?) - Тип сутності
- EntityId (int?) - ID сутності
- Details (string?) - Деталі (JSON)
- Timestamp (DateTime) - Час дії
- IpAddress (string?) - IP адреса
```

### AdminAction (Enum)
```csharp
- ViewedAppeal - Переглянув звернення
- RepliedToAppeal - Відповів на звернення
- ClosedAppeal - Закрив звернення
- ReassignedAppeal - Переадресував звернення
- PublishedNews - Опублікував новину
- EditedContent - Відредагував контент
- BannedUser - Заблокував користувача
- UnbannedUser - Розблокував користувача
- ChangedUserRole - Змінив роль користувача
```

### RateLimitEntry (Запис для rate limiting)
```csharp
- Id (int, PK) - Ідентифікатор
- UserId (long) - ID користувача
- Action (string) - Тип дії (CreateAppeal, SendMessage)
- Timestamp (DateTime) - Час дії
- ExpiresAt (DateTime) - Час закінчення
```

---

## 🔄 Архітектура (Clean Architecture + CQRS)

### Структура проекту
```
StudentUnionBot/
├── Domain/                      # Бізнес-логіка та моделі
│   ├── Entities/               # Моделі даних
│   ├── Enums/                  # Enum типи
│   ├── Interfaces/             # Інтерфейси репозиторіїв
│   └── ValueObjects/           # Value objects
│
├── Application/                 # Application layer (CQRS)
│   ├── Commands/               # Команди (зміна стану)
│   ├── Queries/                # Запити (читання даних)
│   ├── DTOs/                   # Data Transfer Objects
│   ├── Validators/             # FluentValidation
│   ├── Mappings/               # AutoMapper profiles
│   └── Interfaces/             # Інтерфейси сервісів
│
├── Infrastructure/              # Інфраструктура
│   ├── Data/                   # DbContext, Migrations
│   ├── Repositories/           # Реалізація репозиторіїв
│   ├── Services/               # Зовнішні сервіси
│   ├── Caching/                # Redis кешування
│   └── Email/                  # Email сервіс
│
├── Presentation/                # Презентаційний шар
│   ├── Bot/                    # Telegram bot handlers
│   │   ├── Handlers/           # Message/Callback handlers
│   │   ├── Keyboards/          # Inline keyboards
│   │   ├── Middlewares/        # Bot middlewares
│   │   └── States/             # User state management
│   ├── Api/                    # HTTP API endpoints
│   │   ├── Controllers/        # API controllers
│   │   └── Webhooks/           # Telegram webhook
│   └── Localization/           # Translations (uk/en)
│
├── Core/                        # Shared
│   ├── Constants/              # Константи
│   ├── Extensions/             # Extension methods
│   ├── Helpers/                # Helper classes
│   └── Exceptions/             # Custom exceptions
│
└── Program.cs                   # Entry point
```

---

## 🔐 Система безпеки

### 1. Rate Limiting
```csharp
// Обмеження на звернення: 1 на 10 хвилин
// Обмеження на повідомлення: 10 на хвилину
// Обмеження на команди: 30 на хвилину
```

### 2. Email Верифікація
```csharp
// 1. Студент вводить університетську пошту
// 2. Надсилається код верифікації (6 цифр)
// 3. Студент вводить код
// 4. Статус IsEmailVerified = true
```

### 3. Валідація даних
```csharp
// FluentValidation для всіх вхідних даних
// Перевірка на SQL injection
// Санітизація HTML
// Максимальна довжина повідомлень
```

### 4. Антиспам
```csharp
// Детекція повторюваного контенту
// Блокування за флуд
// Captcha при підозрілій активності
```

---

## 🌐 Середовища розробки

### Development (Локальна розробка)
- SQLite база даних: `Data/studentunion_dev.db`
- In-memory Redis (опціонально)
- Webhook через ngrok
- Тестовий бот (окремий токен)
- Конфігурація: `appsettings.Development.json`
- Serilog логування в файли та консоль

### Production (Render.com)
- PostgreSQL база даних
- Redis Cloud для кешування
- HTTPS Webhook
- Продакшн бот
- Конфігурація: змінні оточення на Render
- Serilog логування з ротацією
- Prometheus метрики
- Health checks

---

## 📈 Метрики та моніторинг

### Prometheus Metrics
```
- bot_messages_total - Загальна кількість повідомлень
- bot_appeals_created_total - Створено звернень
- bot_response_time_seconds - Час відповіді
- bot_active_users_count - Активні користувачі
- bot_errors_total - Кількість помилок
- bot_webhook_requests_total - Webhook запити
```

### Health Checks
```
- Database connectivity
- Redis connectivity
- Telegram API availability
- Disk space
- Memory usage
```

---

## 🎨 Принципи розробки (Оновлені)

1. **Clean Architecture** - розділення на шари
2. **CQRS Pattern** - розділення команд та запитів
3. **Dependency Injection** - всі залежності через DI
4. **Repository Pattern** - абстракція доступу до даних
5. **Unit of Work** - транзакційність операцій
6. **Domain-Driven Design** - бізнес-логіка в Domain layer
7. **SOLID Principles** - дотримання принципів ООП
8. **Async/Await** - всі операції асинхронні
9. **Error Handling** - глобальний error handler
10. **Logging** - структуроване логування
11. **Validation** - FluentValidation для всіх вхідних даних
12. **Testing** - Unit + Integration тести
13. **Performance** - кешування, AsNoTracking, pagination
14. **Security** - rate limiting, validation, email verification

---

## 📝 Приклад використання (CQRS)

### Створення звернення (Command)
```csharp
// Command
public record CreateAppealCommand(
    long StudentId,
    AppealCategory Category,
    string Subject,
    string Message
) : IRequest<Result<AppealDto>>;

// Handler
public class CreateAppealCommandHandler 
    : IRequestHandler<CreateAppealCommand, Result<AppealDto>>
{
    private readonly IAppealRepository _repository;
    private readonly IValidator<CreateAppealCommand> _validator;
    private readonly IRateLimiter _rateLimiter;
    
    public async Task<Result<AppealDto>> Handle(
        CreateAppealCommand request, 
        CancellationToken ct)
    {
        // 1. Валідація
        var validation = await _validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Result.Fail(validation.Errors);
            
        // 2. Rate limiting
        if (!await _rateLimiter.AllowAsync(request.StudentId, "CreateAppeal"))
            return Result.Fail("RateLimitExceeded");
            
        // 3. Створення звернення
        var appeal = Appeal.Create(/* ... */);
        await _repository.AddAsync(appeal, ct);
        
        // 4. Відправка сповіщень адмінам
        await _notificationService.NotifyAdminsAsync(appeal, ct);
        
        return Result.Ok(appeal.ToDto());
    }
}
```

### Отримання звернень (Query)
```csharp
// Query
public record GetActiveAppealsQuery(
    AppealCategory? Category = null,
    int Page = 1,
    int PageSize = 10
) : IRequest<Result<PagedResult<AppealDto>>>;

// Handler
public class GetActiveAppealsQueryHandler 
    : IRequestHandler<GetActiveAppealsQuery, Result<PagedResult<AppealDto>>>
{
    private readonly IAppealRepository _repository;
    private readonly IMapper _mapper;
    
    public async Task<Result<PagedResult<AppealDto>>> Handle(
        GetActiveAppealsQuery request, 
        CancellationToken ct)
    {
        var appeals = await _repository
            .GetActiveAppealsAsync(request.Category, ct);
            
        var pagedResult = appeals
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();
            
        var dtos = _mapper.Map<List<AppealDto>>(pagedResult);
        
        return Result.Ok(new PagedResult<AppealDto>(
            dtos, 
            appeals.Count, 
            request.Page, 
            request.PageSize
        ));
    }
}
```

---

## 🚀 CI/CD Pipeline

```yaml
# .github/workflows/deploy.yml
name: Deploy to Render

on:
  push:
    branches: [ main ]

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
      - name: Run tests
        run: dotnet test
        
  deploy:
    needs: test
    runs-on: ubuntu-latest
    steps:
      - name: Deploy to Render
        run: |
          curl -X POST ${{ secrets.RENDER_DEPLOY_HOOK }}
```

---

**Версія документа:** 2.0  
**Дата:** 08.10.2025  
**Автор:** AI Assistant  
**Зміни:** Додано CQRS, Clean Architecture, покращена безпека, метрики, багатомовність
