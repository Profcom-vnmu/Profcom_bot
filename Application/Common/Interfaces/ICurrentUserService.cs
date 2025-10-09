namespace StudentUnionBot.Application.Common.Interfaces;

/// <summary>
/// Сервіс для отримання інформації про поточного користувача
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// ID поточного користувача (Telegram ID)
    /// </summary>
    long? UserId { get; }

    /// <summary>
    /// Ім'я користувача
    /// </summary>
    string? Username { get; }

    /// <summary>
    /// Встановити поточного користувача
    /// </summary>
    void SetCurrentUser(long userId, string? username = null);

    /// <summary>
    /// Очистити поточного користувача
    /// </summary>
    void Clear();
}