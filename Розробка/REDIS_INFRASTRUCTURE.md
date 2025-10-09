# Redis Caching Infrastructure Documentation

## Огляд

Redis Caching Infrastructure забезпечує високопродуктивне кешування для StudentUnionBot з підтримкою graceful degradation при недоступності Redis серверу.

## Архітектура

### Основні компоненти

1. **IRedisCacheService** - Базовий інтерфейс для роботи з Redis
2. **RedisCacheService** - Реалізація Redis операцій з обробкою помилок
3. **IStudentUnionCacheService** - Специфічний інтерфейс для кешування даних проекту
4. **StudentUnionCacheService** - Реалізація кешування з логікою проекту
5. **RedisHealthCheck** - Моніторинг стану Redis підключення

### Структура кешування

```
user_state:{userId}          - Стан користувача (24 години)
news_list:{page}:{size}      - Список новин (30 хвилин)
news:{newsId}                - Окрема новина (30 хвилин)
events_list:{page}:{size}:{filter} - Список подій (15 хвилин)
event:{eventId}              - Окрема подія (15 хвилин)
event_participants:{eventId} - Учасники події (15 хвилин)
user:{userId}                - Дані користувача (1 година)
rate_limit:{userId}          - Rate limiting (динамічно)
statistics:{name}            - Статистика (7 днів)
temp:{key}                   - Тимчасові дані (динамічно)
```

## Конфігурація

### appsettings.json
```json
{
  "ConnectionStrings": {
    "Redis": "localhost:6379"
  }
}
```

### Для продакшену (render.yaml)
```yaml
env:
  - key: REDIS_CONNECTION_STRING
    value: "redis://username:password@host:port"
```

### Environment Variables
- `ConnectionStrings__Redis` - Рядок підключення до Redis
- `Redis__InstanceName` - Назва інстансу (за замовчуванням: "StudentUnionBot")

## Використання

### Базові операції (IRedisCacheService)

```csharp
// Збереження
await _cacheService.SetAsync("key", data, TimeSpan.FromMinutes(30));

// Отримання
var data = await _cacheService.GetAsync<MyClass>("key");

// Видалення
await _cacheService.RemoveAsync("key");

// Видалення за патерном
await _cacheService.RemoveByPatternAsync("news_*");

// Інкремент
await _cacheService.IncrementAsync("counter", 3600); // TTL в секундах
```

### Специфічні операції (IStudentUnionCacheService)

#### Кешування стану користувачів
```csharp
// Встановити стан
await _cacheService.SetUserStateAsync(userId, "waiting_for_input");

// Отримати стан
var state = await _cacheService.GetUserStateAsync(userId);

// Видалити стан
await _cacheService.RemoveUserStateAsync(userId);
```

#### Кешування новин
```csharp
// Кешування списку
var newsList = await _newsRepository.GetPublishedNewsAsync(...);
await _cacheService.SetNewsListAsync(page, pageSize, newsList);

// Отримання з кешу
var cached = await _cacheService.GetNewsListAsync<NewsListDto>(page, pageSize);

// Інвалідація
await _cacheService.InvalidateNewsAsync(); // Всі новини
await _cacheService.InvalidateNewsAsync(newsId); // Конкретна новина
```

#### Rate Limiting
```csharp
// Перевірка ліміту
var isLimited = await _cacheService.IsUserRateLimitedAsync(
    userId, 
    maxRequests: 10, 
    window: TimeSpan.FromMinutes(1)
);
```

#### Статистика
```csharp
// Інкремент
await _cacheService.IncrementStatisticAsync("user_registrations");

// Отримання
var count = await _cacheService.GetStatisticAsync("user_registrations");
```

## Інтеграція з MediatR

### Query Handlers з кешуванням

```csharp
public class GetPublishedNewsQueryHandler
{
    public async Task<Result<NewsListDto>> Handle(GetPublishedNewsQuery request)
    {
        // Спробувати отримати з кешу
        var cached = await _cacheService.GetNewsListAsync<NewsListDto>(
            request.PageNumber, request.PageSize);
        
        if (cached != null)
            return Result<NewsListDto>.Ok(cached);

        // Завантажити з БД
        var data = await _repository.GetDataAsync(...);
        
        // Закешувати результат
        await _cacheService.SetNewsListAsync(request.PageNumber, request.PageSize, data);
        
        return Result<NewsListDto>.Ok(data);
    }
}
```

### Command Handlers з інвалідацією

```csharp
public class CreateNewsCommandHandler
{
    public async Task<Result<NewsDto>> Handle(CreateNewsCommand request)
    {
        // Створити новину
        var news = await _repository.CreateAsync(...);
        
        // Інвалідувати кеш
        await _cacheService.InvalidateNewsAsync();
        
        return Result<NewsDto>.Ok(newsDto);
    }
}
```

## Graceful Degradation

Система продовжує працювати навіть при недоступності Redis:

```csharp
public async Task<T?> GetAsync<T>(string key) where T : class
{
    try
    {
        // Спроба роботи з Redis
        return await _redis.GetAsync<T>(key);
    }
    catch (Exception ex)
    {
        _logger.LogWarning(ex, "Redis unavailable, falling back to non-cached operation");
        return null; // Продовжити без кешу
    }
}
```

## Моніторинг

### Health Checks

- **Endpoint**: `/health`
- **Redis Check**: Тестування операцій запису/читання/видалення
- **Статуси**: Healthy, Degraded, Unhealthy

### Логування

```csharp
// Успішні операції
_logger.LogDebug("Cache hit for key: {Key}", key);
_logger.LogDebug("Cache miss for key: {Key}, fetching from source", key);

// Помилки
_logger.LogWarning(ex, "Redis operation failed, continuing without cache");
_logger.LogError(ex, "Critical Redis error: {Message}", ex.Message);
```

### Метрики

- Cache hit/miss ratio
- Response times
- Error rates
- Memory usage

## Performance

### Рекомендовані TTL

| Тип даних | TTL | Обґрунтування |
|-----------|-----|---------------|
| User State | 24 години | Сесійні дані |
| News List | 30 хвилин | Середня частота оновлень |
| Event List | 15 хвилин | Динамічні дані реєстрації |
| User Data | 1 година | Профільні дані |
| Statistics | 7 днів | Агреговані дані |
| Rate Limiting | 1-60 хвилин | Залежно від правил |

### Оптимізації

1. **Серіалізація**: JSON (System.Text.Json) для баланс швидкості/розміру
2. **Compression**: Не використовується (компроміс розмір/швидкість)
3. **Connection Pooling**: StackExchange.Redis connection multiplexer
4. **Pipeline Operations**: Для bulk операцій

## Deployment

### Development

```bash
# Локальний Redis
docker run -d -p 6379:6379 redis:alpine

# Конфігурація
"ConnectionStrings": {
  "Redis": "localhost:6379"
}
```

### Production (Render.com)

1. Додати Redis Add-on до Render проекту
2. Встановити environment variable:
   ```
   CONNECTIONSTRINGS__REDIS=redis://username:password@host:port
   ```

### Environment Variables Priority

1. `CONNECTIONSTRINGS__REDIS`
2. `ConnectionStrings__Redis` в appsettings.json
3. Fallback: null (кеш відключено)

## Troubleshooting

### Типові проблеми

1. **Connection Timeout**
   ```
   Symptom: TimeoutException в логах
   Solution: Перевірити мережеве підключення, збільшити timeout
   ```

2. **Memory Issues**
   ```
   Symptom: OutOfMemoryException в Redis
   Solution: Налаштувати eviction policy, зменшити TTL
   ```

3. **Serialization Errors**
   ```
   Symptom: JsonException при десеріалізації
   Solution: Перевірити сумісність моделей, очистити кеш
   ```

### Діагностика

```bash
# Підключення до Redis CLI
redis-cli -h host -p port -a password

# Перевірка ключів
KEYS student_union_*

# Інформація про пам'ять
INFO memory

# Статистика
INFO stats
```

## Безпека

1. **Connection String**: Зберігати в environment variables
2. **Auth**: Використовувати password authentication
3. **Network**: TLS для production connections
4. **Data**: Не зберігати sensitive data в кеші

## Масштабування

### Горизонтальне

1. Redis Cluster для розподіленого кешування
2. Consistent hashing для розподілу ключів
3. Replication для високої доступності

### Вертикальне

1. Збільшення RAM Redis серверу
2. CPU оптимізація для серіалізації
3. Network bandwidth для bulk operations

---

**Версія документації**: 1.0  
**Остання модифікація**: 2025-01-09  
**Автор**: AI Assistant / StudentUnionBot Team