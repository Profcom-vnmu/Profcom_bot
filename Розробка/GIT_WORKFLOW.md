# 🔀 Git Workflow - Структура гілок

## 📋 Огляд гілок

### 🚀 `production`
- **Призначення**: Стабільна версія для production deployment на Render.com
- **Бот**: Production bot (основний токен)
- **База даних**: PostgreSQL на Render
- **Deployment**: Автоматичний деплой на Render.com
- **Оновлення**: Тільки через Pull Request з `development` після тестування

### 🛠️ `development`
- **Призначення**: Активна розробка та тестування нових функцій
- **Бот**: Test bot (ID: 8464698453)
- **База даних**: Локальна SQLite (`studentunion_dev.db`)
- **Робота**: Всі нові комміти, експерименти, розробка
- **Тестування**: Локальне тестування перед merge в production

### 📚 `main` (legacy)
- **Статус**: Буде видалена після переходу на нову структуру
- **Використання**: Не використовується

## 🔄 Workflow для розробки

### 1️⃣ Початок роботи
```bash
# Переключитися на гілку розробки
git checkout development

# Переконатися що у вас остання версія
git pull origin development
```

### 2️⃣ Розробка нової функції
```bash
# Всі зміни робимо в development
git add .
git commit -m "✨ Опис нової функції"
git push origin development
```

### 3️⃣ Локальне тестування
```bash
# Запуск з тестовим ботом
dotnet run --environment Development

# Або через VS Code (автоматично використає Development)
F5
```

### 4️⃣ Деплой в production
```bash
# Коли функція протестована і готова
git checkout production
git merge development

# Перевірка перед push
git log --oneline -5

# Деплой в production
git push origin production

# Повернутися на development для подальшої роботи
git checkout development
```

## ⚙️ Конфігурація середовищ

### Development (локальна розробка)
- **Файл**: `appsettings.Development.json`
- **Токен**: Test bot (8464698453:AAFSo2z193xJTMRjLmE5AjkGtzsrKZZJwXo)
- **БД**: SQLite локально
- **Логування**: Debug рівень

### Production (Render.com)
- **Файл**: `appsettings.Production.json` + environment variables
- **Токен**: Production bot (через RENDER_BOT_TOKEN env var)
- **БД**: PostgreSQL (через DATABASE_URL env var)
- **Логування**: Information рівень

## 🛡️ Правила безпеки

### ✅ DO:
- Коміти робимо тільки в `development`
- Тестуємо локально перед merge
- Використовуємо тестовий бот для розробки
- Пишемо зрозумілі commit messages

### ❌ DON'T:
- Не комітимо токени в Git
- Не пушимо безпосередньо в `production` (тільки через merge)
- Не використовуємо production бот локально
- Не комітимо бази даних (.db файли)

## 📝 Приклади commit messages

### Нові функції
```bash
git commit -m "✨ Додано підтримку inline-кнопок"
git commit -m "✨ Реалізовано багатомовність UA/EN"
```

### Виправлення
```bash
git commit -m "🐛 Виправлено помилку при закритті звернення"
git commit -m "🔧 Оновлено конфігурацію для PostgreSQL"
```

### Рефакторинг
```bash
git commit -m "♻️ Рефакторинг обробників звернень"
git commit -m "🎨 Покращено форматування коду"
```

### Документація
```bash
git commit -m "📝 Додано README для Git workflow"
git commit -m "📝 Оновлено документацію API"
```

## 🚨 Що робити при конфлікті?

```bash
# Якщо є конфлікти при merge
git checkout production
git merge development

# Якщо є конфлікти - вирішити їх
git status  # подивитися конфліктні файли
# Відредагувати файли, видалити маркери конфлікту
git add .
git commit -m "🔀 Merge development → production"
git push origin production

# Повернутися на development
git checkout development
```

## 📊 Перевірка стану

```bash
# Подивитися поточну гілку
git branch

# Подивитися останні комміти
git log --oneline -10

# Різниця між гілками
git diff development production

# Статус файлів
git status
```

## 🔗 Корисні посилання

- [Render Dashboard](https://dashboard.render.com/)
- [GitHub Repository](https://github.com/Profcom-vnmu/Profcom_bot)
- [Telegram Bot API](https://core.telegram.org/bots/api)

## 📞 Test Bot

- **ID**: `8464698453`
- **Token**: `AAFSo2z193xJTMRjLmE5AjkGtzsrKZZJwXo`
- **Username**: `@YourTestBotUsername` (встановити після створення)
- **Призначення**: Локальне тестування в development

---

**Останнє оновлення**: 8 жовтня 2025  
**Автор**: Development Team
