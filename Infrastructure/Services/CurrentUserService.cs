using StudentUnionBot.Application.Common.Interfaces;

namespace StudentUnionBot.Infrastructure.Services;

/// <summary>
/// Реалізація сервісу поточного користувача з використанням AsyncLocal
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    private static readonly AsyncLocal<UserContext?> _userContext = new();

    public long? UserId => _userContext.Value?.UserId;
    public string? Username => _userContext.Value?.Username;

    public void SetCurrentUser(long userId, string? username = null)
    {
        _userContext.Value = new UserContext
        {
            UserId = userId,
            Username = username
        };
    }

    public void Clear()
    {
        _userContext.Value = null;
    }

    private class UserContext
    {
        public long UserId { get; set; }
        public string? Username { get; set; }
    }
}