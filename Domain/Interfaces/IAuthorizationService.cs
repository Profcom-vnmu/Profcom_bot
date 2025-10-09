using StudentUnionBot.Domain.Enums;

namespace StudentUnionBot.Domain.Interfaces;

/// <summary>
/// Сервіс для перевірки дозволів користувачів
/// </summary>
public interface IAuthorizationService
{
    /// <summary>
    /// Перевірити чи має користувач конкретний дозвіл
    /// </summary>
    Task<bool> HasPermissionAsync(long userId, Permission permission, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Перевірити чи має користувач будь-який з переданих дозволів
    /// </summary>
    Task<bool> HasAnyPermissionAsync(long userId, CancellationToken cancellationToken = default, params Permission[] permissions);
    
    /// <summary>
    /// Перевірити чи має користувач всі передані дозволи
    /// </summary>
    Task<bool> HasAllPermissionsAsync(long userId, CancellationToken cancellationToken = default, params Permission[] permissions);
    
    /// <summary>
    /// Отримати всі дозволи користувача
    /// </summary>
    Task<IReadOnlyList<Permission>> GetUserPermissionsAsync(long userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Отримати роль користувача
    /// </summary>
    Task<UserRole?> GetUserRoleAsync(long userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Перевірити чи є користувач адміністратором
    /// </summary>
    Task<bool> IsAdminAsync(long userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Перевірити чи є користувач суперадміністратором
    /// </summary>
    Task<bool> IsSuperAdminAsync(long userId, CancellationToken cancellationToken = default);
}