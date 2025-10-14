# 🤖 Telegram Bot Студентської Спілки

Багатофункціональний Telegram бот для студентської спілки з системою звернень, новин, банів та адміністративним управлінням.

---

## ⚡ Швидкий старт

### Локальний запуск

1. **Клонуйте репозиторій:**
   ```bash
   git clone <your-repo-url>
   cd StudentUnionBot
   ```

2. **Налаштуйте токен:**
   - Відкрийте `appsettings.json`
   - Замініть `YOUR_BOT_TOKEN` на токен з @BotFather

3. **Додайте себе як адміністратора:**
   - Дізнайтесь свій Telegram ID (@userinfobot)
   - Додайте ID в `admins.txt`

4. **Запустіть бота:**
   ```bash
   dotnet run
   ```

---

## ☁️ Деплой в хмару (24/7)

### 🥇 Railway.app (рекомендовано)
Найпростіший варіант - працює 24/7, не засинає.

📖 **Повна інструкція:** [DEPLOY_RAILWAY.md](DEPLOY_RAILWAY.md)

### 🥈 Render.com (без карти)
Безкоштовно без карти, але засинає після 15 хв.

📖 **Повна інструкція:** [DEPLOY_RENDER.md](DEPLOY_RENDER.md)

### 📊 Порівняння всіх хостингів
Детальне порівняння Railway, Render, Fly.io, Oracle Cloud та інших.

📖 **Повне порівняння:** [HOSTING_COMPARISON.md](HOSTING_COMPARISON.md)

---

## � Налаштування Environment Variables

### 🔑 Для Render.com / Railway.app

Замість файлів `admins.txt` та `ban.txt` можна використовувати Environment Variables:

#### Обов'язкові змінні:
```bash
BotToken=YOUR_BOT_TOKEN_HERE
DATABASE_URL=postgresql://...   # Для production
```

#### Опціональні змінні:
```bash
# Адміністратори (розділені комами)
ADMIN_IDS=123456789,987654321,555666777

# Забанені користувачі (розділені комами)
BANNED_USER_IDS=111222333,444555666

# Локальна БД (для розробки)
DatabasePath=Data/studentunion.db
```

#### Формати ADMIN_IDS:
```bash
# Різні підтримувані формати:
ADMIN_IDS=123456789,987654321           # Кома
ADMIN_IDS=123456789;987654321           # Крапка з комою  
ADMIN_IDS=123456789 987654321           # Пробіл
```

### 🏠 Для локальної розробки

Альтернативно створіть файли:
```
admins.txt      # Один Telegram ID на рядок
ban.txt         # Один Telegram ID на рядок (опціонально)
```

**Перевага Environment Variables:**
- ✅ Безпечніше (немає конфіденційних даних в Git)
- ✅ Легше оновлювати адмінів без передеплою
- ✅ Стандартний підхід для cloud hosting

---

## �📦 Запуск на Windows без VS Code

Для звичайних користувачів створено окрему папку з executable файлом:

```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

Результат у папці: `bin\Release\net8.0\win-x64\publish\`

**Файли:**
- `StudentUnionBot.exe` - виконуваний файл
- `ЗАПУСК_БОТА.bat` - зручний запуск
- `ПОЧНІТЬ_ЗВІДСИ.txt` - інструкція
- `README.txt` - повна документація
- `Додати_в_автозапуск.bat` - автозапуск Windows

---

## 🎯 Функціонал

### Для студентів:
- 📢 **Новини** - перегляд актуальних новин
- 📩 **Звернення** - створення та відстеження звернень
- 📞 **Контакти** - контактна інформація спілки
- 🏠 **Гуртожиток** - інформація про гуртожиток
- 🌟 **Можливості:**
  - 🤝 Партнери студентської спілки
  - 🎉 Заходи та активності
  - 💡 Запропонувати захід (Google Form)

### Для адміністраторів:
- ➕ **Створити новину** - публікація новин
- 📋 **Переглянути звернення** - управління зверненнями
- 💬 **Відповісти на звернення** - комунікація зі студентами
- 📝 **Редагувати контакти** - оновлення контактної інформації
- 🤝 **Редагувати партнерів** - управління списком партнерів
- 🎉 **Редагувати заходи** - оновлення інформації про заходи
- 🚫 **Система банів** - блокування зловмисників через ban.txt

---

## 🗂️ Структура проекту

```
StudentUnionBot/
├── Data/
│   ├── BotDbContext.cs          # Контекст бази даних
│   ├── BotDbContextFactory.cs   # Фабрика для міграцій
│   └── studentunion.db          # SQLite база даних
├── Migrations/                   # EF Core міграції
├── Models/
│   ├── Appeal.cs                # Модель звернення
│   ├── AppealMessage.cs         # Повідомлення звернення
│   ├── BotUser.cs               # Користувач
│   ├── CommandType.cs           # Типи команд
│   ├── News.cs                  # Новини
│   ├── PartnersInfo.cs          # Інформація про партнерів
│   └── EventsInfo.cs            # Інформація про заходи
├── Services/
│   ├── AppealService.cs         # Сервіс звернень
│   ├── BotService.cs            # Основна логіка бота
│   ├── NewsService.cs           # Сервіс новин
│   ├── UserService.cs           # Сервіс користувачів + бани
│   └── UserState.cs             # Стан користувача
├── Program.cs                    # Точка входу
├── appsettings.json             # Конфігурація
├── admins.txt                   # Список адміністраторів
├── ban.txt                      # Заблоковані користувачі
├── Dockerfile                   # Docker конфігурація
├── .dockerignore                # Виключення для Docker
└── render.yaml                  # Конфігурація Render.com
```

---

## 🛠️ Технології

- **.NET 8.0** - фреймворк
- **Telegram.Bot 19.0.0** - Telegram Bot API
- **Entity Framework Core 8.0** - ORM
- **SQLite** - база даних
- **Docker** - контейнеризація

---

## ⚙️ Конфігурація

### appsettings.json
```json
{
  "BotConfiguration": {
    "BotToken": "YOUR_BOT_TOKEN",
    "DatabasePath": "Data/studentunion.db"
  }
}
```

### Змінні середовища (для хостингу)
```bash
BotToken=your_token_here
DatabasePath=Data/studentunion.db  # опціонально
```

### admins.txt
```
831894804
123456789
# Коментарі починаються з #
```

### ban.txt
```
987654321  # Порушник правил
111222333  # Спамер
```

---

## 🚀 Команди розробки

```bash
# Запуск
dotnet run

# Білд
dotnet build

# Публікація (Windows executable)
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true

# Міграції
dotnet ef migrations add MigrationName
dotnet ef database update

# Docker
docker build -t studentunionbot .
docker run -e BotToken="your_token" studentunionbot
```

---

## 📊 База даних

### Таблиці:
- **Users** - користувачі бота
- **Appeals** - звернення студентів
- **AppealMessages** - повідомлення в зверненнях (текст/фото/файли)
- **News** - новини спілки
- **ContactInfo** - контактна інформація
- **PartnersInfo** - інформація про партнерів
- **EventsInfo** - інформація про заходи

---

## 🔒 Безпека

- ✅ Токен бота зберігається у змінних середовища
- ✅ Токен НЕ включається в Git (через .gitignore)
- ✅ Система банів через ban.txt
- ✅ Ідентифікація адміністраторів через Telegram ID
- ✅ Перевірка прав доступу для адмін-команд

---

## 🐛 Відомі обмеження

- База даних SQLite (для продакшену краще PostgreSQL)
- Файли ban.txt та admins.txt на сервері треба оновлювати вручну
- Медіа файли зберігаються тільки як file_id (залежать від Telegram)

---

## 📝 Ліцензія

MIT License - можете використовувати вільно.

---

## 👥 Автори

Створено для Студентської Спілки ВНМУ  
Версія 5.5 | Жовтень 2025

---

## 🆘 Підтримка

**Проблеми з локальним запуском:**
- Перевірте чи встановлено .NET 8 SDK
- `dotnet --version` має показувати 8.x.x

**Проблеми з деплоєм:**
- Перегляньте відповідний DEPLOY_*.md файл
- Перевірте логи на хостингу

**Питання щодо функціоналу:**
- Перегляньте код в Services/BotService.cs
- Всі команди описані в HandleUpdateAsync

---

**Успішного деплою! 🚀**
