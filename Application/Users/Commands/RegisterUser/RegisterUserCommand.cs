using MediatR;
using StudentUnionBot.Application.Users.DTOs;
using StudentUnionBot.Core.Results;
using StudentUnionBot.Domain.Enums;

namespace StudentUnionBot.Application.Users.Commands.RegisterUser;

/// <summary>
/// Команда для реєстрації нового користувача або оновлення базової інформації
/// </summary>
public class RegisterUserCommand : IRequest<Result<UserDto>>
{
    /// <summary>
    /// Telegram ID користувача
    /// </summary>
    public long TelegramId { get; set; }
    
    /// <summary>
    /// Username в Telegram (без @)
    /// </summary>
    public string? Username { get; set; }
    
    /// <summary>
    /// Ім'я користувача
    /// </summary>
    public string? FirstName { get; set; }
    
    /// <summary>
    /// Прізвище користувача
    /// </summary>
    public string? LastName { get; set; }
    
    /// <summary>
    /// Мова інтерфейсу (за замовчуванням Ukrainian)
    /// </summary>
    public Language Language { get; set; } = Language.Ukrainian;
}
