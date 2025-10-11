# API Development Prompt

**Мета:** Цей файл містить інструкції для створення REST API ендпоінтів у проєкті StudentUnionBot.

## Контекст проєкту
Проєкт використовує Clean Architecture + CQRS. API контролери повинні бути "тонкими" - тільки валідація вхідних даних, виклик MediatR команд/запитів, обробка Result<T>.

---

## Структура API Controller

```csharp
[ApiController]
[Route("api/[controller]")]
public class {EntityName}Controller : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<{EntityName}Controller> _logger;

    public {EntityName}Controller(IMediator mediator, ILogger<{EntityName}Controller> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    // GET endpoints - use Queries
    [HttpGet("{id}")]
    [ProducesResponseType(typeof({EntityName}Dto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var query = new Get{EntityName}ByIdQuery(id);
        var result = await _mediator.Send(query, cancellationToken);

        return result.IsSuccess 
            ? Ok(result.Value) 
            : NotFound(result.ErrorMessage);
    }

    // POST endpoints - use Commands
    [HttpPost]
    [ProducesResponseType(typeof({EntityName}Dto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] Create{EntityName}Command command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            _logger.LogWarning("Failed to create {EntityName}: {Error}", typeof({EntityName}).Name, result.ErrorMessage);
            return BadRequest(result.ErrorMessage);
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value);
    }

    // PUT endpoints - use Commands
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] Update{EntityName}Command command, CancellationToken cancellationToken)
    {
        if (id != command.Id)
            return BadRequest("ID mismatch");

        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
            return NotFound(result.ErrorMessage);

        return NoContent();
    }

    // DELETE endpoints - use Commands
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var command = new Delete{EntityName}Command(id);
        var result = await _mediator.Send(command, cancellationToken);

        return result.IsSuccess ? NoContent() : NotFound(result.ErrorMessage);
    }
}
```

---

## Правила для API

### 1. Dependency Injection
- ✅ Ін'єктуй `IMediator` для виконання команд/запитів
- ✅ Ін'єктуй `ILogger<T>` для логування
- ❌ НЕ ін'єктуй репозиторії або DbContext напряму

### 2. Result Pattern
- ✅ Обробляй `Result<T>` від MediatR
- ✅ Повертай правильні HTTP коди:
  - `200 OK` - успішний GET/PUT
  - `201 Created` - успішний POST
  - `204 NoContent` - успішний DELETE/PUT без тіла
  - `400 BadRequest` - валідаційні помилки
  - `404 NotFound` - ресурс не знайдено
  - `500 InternalServerError` - виключення (через middleware)

### 3. Атрибути
```csharp
[ApiController]                    // Автоматична валідація ModelState
[Route("api/[controller]")]        // /api/appeals, /api/events
[Authorize]                        // Якщо потрібна авторизація
[ProducesResponseType]             // Для Swagger документації
```

### 4. Валідація
- FluentValidation валідує команди автоматично (через MediatR Pipeline Behavior)
- Контролер тільки перевіряє прості речі (ID mismatch)

### 5. Async/Await
- ✅ Всі методи `async Task<IActionResult>`
- ✅ Приймай `CancellationToken` останнім параметром
- ✅ Передавай `cancellationToken` у `_mediator.Send()`

### 6. Logging
```csharp
_logger.LogInformation("User {UserId} created {EntityName} {EntityId}", userId, entityName, entityId);
_logger.LogWarning("Failed to update {EntityName} {Id}: {Error}", entityName, id, error);
```

---

## Приклад: Appeals API

```csharp
[ApiController]
[Route("api/appeals")]
public class AppealsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<AppealsController> _logger;

    public AppealsController(IMediator mediator, ILogger<AppealsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<AppealDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new GetAppealsQuery(page, pageSize);
        var result = await _mediator.Send(query, cancellationToken);

        return Ok(result.Value);
    }

    [HttpPost]
    [ProducesResponseType(typeof(AppealDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateAppeal(
        [FromBody] CreateAppealCommand command, 
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            _logger.LogWarning("Appeal creation failed: {Error}", result.ErrorMessage);
            return BadRequest(result.ErrorMessage);
        }

        _logger.LogInformation("Appeal {AppealId} created by user {UserId}", 
            result.Value.Id, command.UserId);

        return CreatedAtAction(nameof(GetById), 
            new { id = result.Value.Id }, 
            result.Value);
    }
}
```

---

## Swagger Configuration

У `Program.cs`:

```csharp
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "StudentUnionBot API", 
        Version = "v1" 
    });
    
    // XML documentation
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
});

// Enable Swagger in Development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
```

---

## Коли створювати API?

⚠️ **Зверни увагу**: Проєкт StudentUnionBot - це Telegram бот, НЕ веб API. 
API контролери потрібні ТІЛЬКИ якщо:
- Потрібен веб-інтерфейс адміністрування
- Інтеграція з зовнішніми системами
- Вебхуки від Telegram (замість long polling)

Для більшості функцій використовуй Telegram Bot Handlers у `Presentation/Bot/Handlers/`.
