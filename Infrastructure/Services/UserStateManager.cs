using System.Collections.Concurrent;
using StudentUnionBot.Application.Common.Interfaces;
using StudentUnionBot.Domain.Enums;

namespace Infrastructure.Services;

/// <summary>
/// In-memory implementation of user state management
/// Note: Data is lost on restart. For production, consider Redis or database storage
/// </summary>
public class UserStateManager : IUserStateManager
{
    private readonly ConcurrentDictionary<long, UserConversationState> _states = new();
    private readonly ConcurrentDictionary<long, ConcurrentDictionary<string, object>> _userData = new();

    public Task SetStateAsync(long userId, UserConversationState state, CancellationToken cancellationToken = default)
    {
        _states[userId] = state;
        return Task.CompletedTask;
    }

    public Task<UserConversationState> GetStateAsync(long userId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_states.GetValueOrDefault(userId, UserConversationState.Idle));
    }

    public Task ClearStateAsync(long userId, CancellationToken cancellationToken = default)
    {
        _states.TryRemove(userId, out _);
        return Task.CompletedTask;
    }

    public Task SetDataAsync<T>(long userId, string key, T value, CancellationToken cancellationToken = default)
    {
        var userDict = _userData.GetOrAdd(userId, _ => new ConcurrentDictionary<string, object>());
        userDict[key] = value!;
        return Task.CompletedTask;
    }

    public Task<T?> GetDataAsync<T>(long userId, string key, CancellationToken cancellationToken = default)
    {
        if (_userData.TryGetValue(userId, out var userDict) && userDict.TryGetValue(key, out var value))
        {
            return Task.FromResult((T?)value);
        }
        return Task.FromResult(default(T));
    }

    public Task RemoveDataAsync(long userId, string key, CancellationToken cancellationToken = default)
    {
        if (_userData.TryGetValue(userId, out var userDict))
        {
            userDict.TryRemove(key, out _);
        }
        return Task.CompletedTask;
    }

    public Task ClearAllDataAsync(long userId, CancellationToken cancellationToken = default)
    {
        _userData.TryRemove(userId, out _);
        return Task.CompletedTask;
    }
}
