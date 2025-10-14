# ⚙️ Налаштування середовищ розробки

## 🏗️ Структура проекту

```
StudentUnionBot/
├── appsettings.json                    # Шаблон (без токена)
├── appsettings.Development.json        # Development config (test bot)
├── appsettings.Production.json         # Production config (template)
├── appsettings.template.json           # Шаблон для копіювання
└── Розробка/
    ├── GIT_WORKFLOW.md                 # Git workflow інструкції
    └── SETUP.md                        # Цей файл
```

## 🚀 Початкове налаштування

### 1. Клонування репозиторію

```bash
git clone https://github.com/Profcom-vnmu/Profcom_bot.git
cd Profcom_bot
```

### 2. Переключення на гілку розробки

```bash
git checkout development
```

### 3. Налаштування конфігурації

**Development** (`appsettings.Development.json`) вже налаштований з тестовим ботом.

Перевірте файл:
```json
{
  "BotConfiguration": {
    "BotToken": "8464698453:AAFSo2z193xJTMRjLmE5AjkGtzsrKZZJwXo",
    "DatabasePath": "Data/studentunion_dev.db"
  }
}
```

### 4. Встановлення залежностей

```bash
dotnet restore
```

### 5. Створення бази даних

```bash
# Автоматично створюється при першому запуску
# Або вручну через EF Core:
dotnet ef database update
```

### 6. Запуск бота

```bash
# Через .NET CLI
dotnet run --environment Development

# Або через VS Code
# Просто натисніть F5 (автоматично використає Development)
```

## 🤖 Налаштування Test Bot в Telegram

1. Знайдіть вашого test бота в Telegram за ID: `8464698453`
2. Або створіть нового через [@BotFather](https://t.me/BotFather):
   ```
   /newbot
   Name: Student Union Test Bot
   Username: your_test_bot_username
   ```
3. Скопіюйте токен та оновіть `appsettings.Development.json`

## 📦 VS Code Launch Configuration

Файл `.vscode/launch.json` (автоматично створюється):

```json
{
  "version": "0.2.0",
  "configurations": [
    {
      "name": ".NET Core Launch (Development)",
      "type": "coreclr",
      "request": "launch",
      "preLaunchTask": "build",
      "program": "${workspaceFolder}/bin/Debug/net8.0/StudentUnionBot.dll",
      "args": [],
      "cwd": "${workspaceFolder}",
      "console": "internalConsole",
      "stopAtEntry": false,
      "env": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  ]
}
```

## 🌍 Environment Variables

### Development (локально)
- `ASPNETCORE_ENVIRONMENT=Development`
- Бот токен: з `appsettings.Development.json`
- БД: SQLite локально

### Production (Render.com)
- `ASPNETCORE_ENVIRONMENT=Production`
- `BotToken`: через Render environment variable
- `DATABASE_URL`: автоматично від PostgreSQL сервісу
- БД: PostgreSQL

## 🗄️ Бази даних

### Development
```
Data/studentunion_dev.db  (SQLite)
```

### Production
```
PostgreSQL на Render.com (через DATABASE_URL)
```

### Міграції

```bash
# Створення нової міграції
dotnet ef migrations add MigrationName

# Застосування міграцій
dotnet ef database update

# Відкат до попередньої міграції
dotnet ef database update PreviousMigrationName

# Видалення останньої міграції
dotnet ef migrations remove
```

## 🔐 Безпека токенів

### ✅ Правильно:
```json
// appsettings.Development.json (локально, в .gitignore)
{
  "BotConfiguration": {
    "BotToken": "8464698453:AAFSo2z193xJTMRjLmE5AjkGtzsrKZZJwXo"
  }
}
```

### ❌ Неправильно:
```json
// Ніколи не комітьте токени в appsettings.json!
{
  "BotConfiguration": {
    "BotToken": "YOUR_REAL_TOKEN_HERE"  // ❌
  }
}
```

## 🧪 Тестування

### Локальне тестування
1. Запустіть бота в Development режимі
2. Знайдіть test бота в Telegram
3. Перевірте всі функції
4. Переконайтеся що все працює

### Перед деплоєм
```bash
# Перевірка компіляції
dotnet build --configuration Release

# Запуск тестів (якщо є)
dotnet test

# Перевірка міграцій
dotnet ef migrations list
```

## 📊 Моніторинг

### Локальні логи
```bash
# Дивитися логи в реальному часі
dotnet run --environment Development
```

### Production логи (Render)
```bash
# Через Render Dashboard
https://dashboard.render.com/
→ Вибрати сервіс
→ Logs
```

## 🆘 Troubleshooting

### Проблема: Бот не відповідає
**Рішення:**
1. Перевірте токен в конфігурації
2. Перевірте чи запущений бот
3. Перевірте логи на помилки

### Проблема: База даних не створюється
**Рішення:**
```bash
# Видалити стару БД та створити нову
rm Data/studentunion_dev.db
dotnet ef database update
```

### Проблема: Міграції конфліктують
**Рішення:**
```bash
# Відкотити всі міграції
dotnet ef database update 0

# Видалити всі міграції
rm -r Migrations/

# Створити нову initial міграцію
dotnet ef migrations add Initial
dotnet ef database update
```

### Проблема: Port вже зайнятий
**Рішення:**
```bash
# Знайти процес на порту 10000
netstat -ano | findstr :10000

# Завершити процес (замініть PID)
taskkill /PID <PID> /F
```

## 🔄 Оновлення з production

```bash
# Якщо production оновлено, синхронізувати з development
git checkout development
git merge production
git push origin development
```

## 📚 Корисні команди

```bash
# Перевірка поточної гілки
git branch

# Статус файлів
git status

# Переключення між гілками
git checkout development
git checkout production

# Pull останніх змін
git pull origin development

# Push змін
git push origin development

# Перегляд логів
git log --oneline -10

# Відкат останнього комміту (локально)
git reset --soft HEAD~1
```

---

**Останнє оновлення**: 8 жовтня 2025  
**Автор**: Development Team
