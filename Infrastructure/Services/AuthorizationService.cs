using Microsoft.Extensions.Logging;
using StudentUnionBot.Domain.Enums;
using StudentUnionBot.Domain.Interfaces;

namespace StudentUnionBot.Infrastructure.Services;

/// <summary>
/// Реалізація сервісу авторизації на основі ролей
/// </summary>
public class AuthorizationService : IAuthorizationService
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<AuthorizationService> _logger;

    public AuthorizationService(
        IUserRepository userRepository,
        ILogger<AuthorizationService> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<bool> HasPermissionAsync(long userId, Permission permission, CancellationToken cancellationToken = default)
    {
        try
        {
            var userRole = await GetUserRoleAsync(userId, cancellationToken);
            if (userRole == null)
            {
                _logger.LogWarning("Користувач {UserId} не знайдений для перевірки дозволу {Permission}", userId, permission);
                return false;
            }

            var hasPermission = userRole.Value.HasPermission(permission);
            
            _logger.LogDebug(
                "Користувач {UserId} з роллю {Role} {HasPermission} дозвіл {Permission}",
                userId,
                userRole,
                hasPermission ? "має" : "не має",
                permission);
                
            return hasPermission;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при перевірці дозволу {Permission} для користувача {UserId}", permission, userId);
            return false;
        }
    }

    public async Task<bool> HasAnyPermissionAsync(long userId, CancellationToken cancellationToken = default, params Permission[] permissions)
    {
        if (permissions == null || permissions.Length == 0)
        {
            return false;
        }

        try
        {
            var userRole = await GetUserRoleAsync(userId, cancellationToken);
            if (userRole == null)
            {
                _logger.LogWarning("Користувач {UserId} не знайдений для перевірки дозволів", userId);
                return false;
            }

            var hasAnyPermission = userRole.Value.HasAnyPermission(permissions);
            
            _logger.LogDebug(
                "Користувач {UserId} з роллю {Role} {HasPermission} будь-який з дозволів: {Permissions}",
                userId,
                userRole,
                hasAnyPermission ? "має" : "не має",
                string.Join(", ", permissions));
                
            return hasAnyPermission;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при перевірці дозволів для користувача {UserId}: {Permissions}", userId, string.Join(", ", permissions));
            return false;
        }
    }

    public async Task<bool> HasAllPermissionsAsync(long userId, CancellationToken cancellationToken = default, params Permission[] permissions)
    {
        if (permissions == null || permissions.Length == 0)
        {
            return true; // Якщо немає дозволів для перевірки, повертаємо true
        }

        try
        {
            var userRole = await GetUserRoleAsync(userId, cancellationToken);
            if (userRole == null)
            {
                _logger.LogWarning("Користувач {UserId} не знайдений для перевірки дозволів", userId);
                return false;
            }

            var hasAllPermissions = userRole.Value.HasAllPermissions(permissions);
            
            _logger.LogDebug(
                "Користувач {UserId} з роллю {Role} {HasPermission} всі дозволи: {Permissions}",
                userId,
                userRole,
                hasAllPermissions ? "має" : "не має",
                string.Join(", ", permissions));
                
            return hasAllPermissions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при перевірці всіх дозволів для користувача {UserId}: {Permissions}", userId, string.Join(", ", permissions));
            return false;
        }
    }

    public async Task<IReadOnlyList<Permission>> GetUserPermissionsAsync(long userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var userRole = await GetUserRoleAsync(userId, cancellationToken);
            if (userRole == null)
            {
                _logger.LogWarning("Користувач {UserId} не знайдений для отримання дозволів", userId);
                return new List<Permission>();
            }

            return userRole.Value.GetPermissions();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при отриманні дозволів користувача {UserId}", userId);
            return new List<Permission>();
        }
    }

    public async Task<UserRole?> GetUserRoleAsync(long userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _userRepository.GetByTelegramIdAsync(userId, cancellationToken);
            return user?.Role;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при отриманні ролі користувача {UserId}", userId);
            return null;
        }
    }

    public async Task<bool> IsAdminAsync(long userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var userRole = await GetUserRoleAsync(userId, cancellationToken);
            return userRole == UserRole.Admin || userRole == UserRole.SuperAdmin;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при перевірці адміністраторських прав користувача {UserId}", userId);
            return false;
        }
    }

    public async Task<bool> IsSuperAdminAsync(long userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var userRole = await GetUserRoleAsync(userId, cancellationToken);
            return userRole == UserRole.SuperAdmin;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при перевірці суперадміністраторських прав користувача {UserId}", userId);
            return false;
        }
    }
}