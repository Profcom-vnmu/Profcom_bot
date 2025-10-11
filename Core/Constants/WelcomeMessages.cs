namespace StudentUnionBot.Core.Constants;

/// <summary>
/// Привітальні повідомлення для команди /start
/// </summary>
public static class WelcomeMessages
{
    /// <summary>
    /// Привітання українською мовою
    /// </summary>
    public const string WelcomeUkrainian = 
        "👋 <b>Вітаємо у боті Профспілки!</b>\n\n" +
        "Я допоможу вам:\n" +
        "🔹 <b>Створити звернення</b> до профкому\n" +
        "🔹 <b>Переглянути новини</b> та оголошення\n" +
        "🔹 <b>Дізнатися про події</b> та зареєструватися\n" +
        "🔹 <b>Знайти контакти</b> профспілкового комітету\n" +
        "🔹 <b>Управляти профілем</b> та налаштуваннями\n\n" +
        "📝 Виберіть дію з меню нижче або використайте команди:\n" +
        "/appeal - Створити звернення\n" +
        "/myappeals - Мої звернення\n" +
        "/news - Новини\n" +
        "/events - Події\n" +
        "/profile - Мій профіль\n" +
        "/contacts - Контакти\n" +
        "/help - Довідка\n\n" +
        "💡 <i>Для скасування будь-якої дії використайте команду /cancel</i>";

    /// <summary>
    /// Привітання англійською мовою
    /// </summary>
    public const string WelcomeEnglish = 
        "👋 <b>Welcome to the Student Union Bot!</b>\n\n" +
        "I will help you:\n" +
        "🔹 <b>Create appeals</b> to the student union\n" +
        "🔹 <b>View news</b> and announcements\n" +
        "🔹 <b>Learn about events</b> and register\n" +
        "🔹 <b>Find contacts</b> of the union committee\n" +
        "🔹 <b>Manage profile</b> and settings\n\n" +
        "📝 Select an action from the menu below or use commands:\n" +
        "/appeal - Create appeal\n" +
        "/myappeals - My appeals\n" +
        "/news - News\n" +
        "/events - Events\n" +
        "/profile - My profile\n" +
        "/contacts - Contacts\n" +
        "/help - Help\n\n" +
        "💡 <i>To cancel any action use /cancel command</i>";

    /// <summary>
    /// Привітання для нового користувача (українська)
    /// </summary>
    public const string WelcomeNewUserUkrainian = 
        "👋 <b>Вітаємо у боті Профспілки ВНМУ!</b>\n\n" +
        "🎓 Це ваш перший візит! Давайте налаштуємо ваш профіль.\n\n" +
        "Я допоможу вам:\n" +
        "🔹 <b>Створювати звернення</b> до профкому\n" +
        "🔹 <b>Отримувати новини</b> та оголошення\n" +
        "🔹 <b>Реєструватися на події</b>\n" +
        "🔹 <b>Зв'язуватися з профспілковим комітетом</b>\n\n" +
        "📝 <b>Для початку роботи:</b>\n" +
        "1️⃣ Заповніть профіль (автоматично)\n" +
        "2️⃣ Підтвердіть студентську email-адресу\n" +
        "3️⃣ Почніть користуватися ботом!\n\n" +
        "💡 <i>Використовуйте /help для отримання довідки</i>";

    /// <summary>
    /// Привітання для нового користувача (англійська)
    /// </summary>
    public const string WelcomeNewUserEnglish = 
        "👋 <b>Welcome to VNMU Student Union Bot!</b>\n\n" +
        "🎓 This is your first visit! Let's set up your profile.\n\n" +
        "I will help you:\n" +
        "🔹 <b>Create appeals</b> to the student union\n" +
        "🔹 <b>Receive news</b> and announcements\n" +
        "🔹 <b>Register for events</b>\n" +
        "🔹 <b>Contact the union committee</b>\n\n" +
        "📝 <b>To get started:</b>\n" +
        "1️⃣ Fill in your profile (automatically)\n" +
        "2️⃣ Verify your student email\n" +
        "3️⃣ Start using the bot!\n\n" +
        "💡 <i>Use /help to get assistance</i>";

    /// <summary>
    /// Отримати привітальне повідомлення залежно від мови та статусу користувача
    /// </summary>
    /// <param name="language">Мова користувача</param>
    /// <param name="isNewUser">Чи є користувач новим</param>
    /// <returns>Привітальне повідомлення</returns>
    public static string GetWelcomeMessage(Domain.Enums.Language language, bool isNewUser = false)
    {
        return language switch
        {
            Domain.Enums.Language.English => isNewUser ? WelcomeNewUserEnglish : WelcomeEnglish,
            _ => isNewUser ? WelcomeNewUserUkrainian : WelcomeUkrainian
        };
    }
}
