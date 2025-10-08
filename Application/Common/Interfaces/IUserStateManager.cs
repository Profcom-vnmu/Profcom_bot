using StudentUnionBot.Domain.Enums;

namespace StudentUnionBot.Application.Common.Interfaces;

/// <summary>
/// Manages user conversation states for multi-step dialogs
/// </summary>
public interface IUserStateManager
{
    /// <summary>
    /// Sets the current state for a user
    /// </summary>
    Task SetStateAsync(long userId, UserConversationState state, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current state for a user
    /// </summary>
    Task<UserConversationState> GetStateAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears the state for a user (returns to idle)
    /// </summary>
    Task ClearStateAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores temporary data for a user during conversation
    /// </summary>
    Task SetDataAsync<T>(long userId, string key, T value, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves temporary data for a user
    /// </summary>
    Task<T?> GetDataAsync<T>(long userId, string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes temporary data for a specific key
    /// </summary>
    Task RemoveDataAsync(long userId, string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears all temporary data for a user
    /// </summary>
    Task ClearAllDataAsync(long userId, CancellationToken cancellationToken = default);
}
