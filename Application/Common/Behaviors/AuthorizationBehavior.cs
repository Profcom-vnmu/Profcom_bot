using MediatR;
using Microsoft.Extensions.Logging;
using System.Reflection;
using StudentUnionBot.Application.Common.Attributes;
using StudentUnionBot.Application.Common.Interfaces;
using StudentUnionBot.Core.Results;
using StudentUnionBot.Domain.Interfaces;

namespace StudentUnionBot.Application.Common.Behaviors;

/// <summary>
/// Pipeline behavior Ð´Ð»Ñ Ð¿ÐµÑ€ÐµÐ²Ñ–Ñ€ÐºÐ¸ Ð°Ð²Ñ‚Ð¾Ñ€Ð¸Ð·Ð°Ñ†Ñ–Ñ— ÐºÐ¾Ð¼Ð°Ð½Ð´ Ñ‚Ð° Ð·Ð°Ð¿Ð¸Ñ‚Ñ–Ð²
/// </summary>
public class AuthorizationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : class
{
    private readonly IAuthorizationService _authorizationService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<AuthorizationBehavior<TRequest, TResponse>> _logger;

    public AuthorizationBehavior(
        IAuthorizationService authorizationService,
        ICurrentUserService currentUserService,
        ILogger<AuthorizationBehavior<TRequest, TResponse>> logger)
    {
        _authorizationService = authorizationService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestType = typeof(TRequest);
        var requestName = requestType.Name;

        // ÐŸÐµÑ€ÐµÐ²Ñ–Ñ€Ð¸Ñ‚Ð¸ Ñ‡Ð¸ Ñ” Ð°Ñ‚Ñ€Ð¸Ð±ÑƒÑ‚Ð¸ Ð°Ð²Ñ‚Ð¾Ñ€Ð¸Ð·Ð°Ñ†Ñ–Ñ—
        var requirePermissionAttr = requestType.GetCustomAttribute<RequirePermissionAttribute>();
        var requireAllPermissionsAttr = requestType.GetCustomAttribute<RequireAllPermissionsAttribute>();
        var requireAdminAttr = requestType.GetCustomAttribute<RequireAdminAttribute>();
        var requireSuperAdminAttr = requestType.GetCustomAttribute<RequireSuperAdminAttribute>();

        // Ð¯ÐºÑ‰Ð¾ Ð½ÐµÐ¼Ð°Ñ” Ð°Ñ‚Ñ€Ð¸Ð±ÑƒÑ‚Ñ–Ð² Ð°Ð²Ñ‚Ð¾Ñ€Ð¸Ð·Ð°Ñ†Ñ–Ñ—, Ð¿Ñ€Ð¾Ð¿ÑƒÑÑ‚Ð¸Ñ‚Ð¸ Ð¿ÐµÑ€ÐµÐ²Ñ–Ñ€ÐºÑƒ
        if (requirePermissionAttr == null && requireAllPermissionsAttr == null && 
            requireAdminAttr == null && requireSuperAdminAttr == null)
        {
            return await next();
        }

        // ÐžÑ‚Ñ€Ð¸Ð¼Ð°Ñ‚Ð¸ Ð¿Ð¾Ñ‚Ð¾Ñ‡Ð½Ð¾Ð³Ð¾ ÐºÐ¾Ñ€Ð¸ÑÑ‚ÑƒÐ²Ð°Ñ‡Ð°
        var currentUserId = _currentUserService.UserId;
        if (currentUserId == null)
        {
            _logger.LogWarning("Ð¡Ð¿Ñ€Ð¾Ð±Ð° Ð²Ð¸ÐºÐ¾Ð½Ð°Ñ‚Ð¸ Ð°Ð²Ñ‚Ð¾Ñ€Ð¸Ð·Ð¾Ð²Ð°Ð½Ñƒ ÐºÐ¾Ð¼Ð°Ð½Ð´Ñƒ {RequestName} Ð±ÐµÐ· Ð¿Ð¾Ñ‚Ð¾Ñ‡Ð½Ð¾Ð³Ð¾ ÐºÐ¾Ñ€Ð¸ÑÑ‚ÑƒÐ²Ð°Ñ‡Ð°", requestName);
            return CreateUnauthorizedResult("ÐšÐ¾Ñ€Ð¸ÑÑ‚ÑƒÐ²Ð°Ñ‡ Ð½Ðµ Ð°Ð²Ñ‚Ð¾Ñ€Ð¸Ð·Ð¾Ð²Ð°Ð½Ð¸Ð¹");
        }

        _logger.LogDebug("ÐŸÐµÑ€ÐµÐ²Ñ–Ñ€ÐºÐ° Ð°Ð²Ñ‚Ð¾Ñ€Ð¸Ð·Ð°Ñ†Ñ–Ñ— Ð´Ð»Ñ ÐºÐ¾Ð¼Ð°Ð½Ð´Ð¸ {RequestName} ÐºÐ¾Ñ€Ð¸ÑÑ‚ÑƒÐ²Ð°Ñ‡Ð° {UserId}", requestName, currentUserId);

        // ÐŸÐµÑ€ÐµÐ²Ñ–Ñ€ÐºÐ° ÑÑƒÐ¿ÐµÑ€Ð°Ð´Ð¼Ñ–Ð½Ñ–ÑÑ‚Ñ€Ð°Ñ‚Ð¾Ñ€Ð°
        if (requireSuperAdminAttr != null)
        {
            var isSuperAdmin = await _authorizationService.IsSuperAdminAsync(currentUserId.Value, cancellationToken);
            if (!isSuperAdmin)
            {
                _logger.LogWarning("ÐšÐ¾Ñ€Ð¸ÑÑ‚ÑƒÐ²Ð°Ñ‡ {UserId} Ð½Ð°Ð¼Ð°Ð³Ð°Ð²ÑÑ Ð²Ð¸ÐºÐ¾Ð½Ð°Ñ‚Ð¸ ÐºÐ¾Ð¼Ð°Ð½Ð´Ñƒ {RequestName}, Ñ‰Ð¾ Ð¿Ð¾Ñ‚Ñ€ÐµÐ±ÑƒÑ” ÑÑƒÐ¿ÐµÑ€Ð°Ð´Ð¼Ñ–Ð½Ñ–ÑÑ‚Ñ€Ð°Ñ‚Ð¾Ñ€Ð°", currentUserId, requestName);
                return CreateUnauthorizedResult("ÐÐµÐ´Ð¾ÑÑ‚Ð°Ñ‚Ð½ÑŒÐ¾ Ð¿Ñ€Ð°Ð² Ð´Ð¾ÑÑ‚ÑƒÐ¿Ñƒ");
            }
        }
        // ÐŸÐµÑ€ÐµÐ²Ñ–Ñ€ÐºÐ° Ð°Ð´Ð¼Ñ–Ð½Ñ–ÑÑ‚Ñ€Ð°Ñ‚Ð¾Ñ€Ð°
        else if (requireAdminAttr != null)
        {
            var isAdmin = await _authorizationService.IsAdminAsync(currentUserId.Value, cancellationToken);
            if (!isAdmin)
            {
                _logger.LogWarning("ÐšÐ¾Ñ€Ð¸ÑÑ‚ÑƒÐ²Ð°Ñ‡ {UserId} Ð½Ð°Ð¼Ð°Ð³Ð°Ð²ÑÑ Ð²Ð¸ÐºÐ¾Ð½Ð°Ñ‚Ð¸ ÐºÐ¾Ð¼Ð°Ð½Ð´Ñƒ {RequestName}, Ñ‰Ð¾ Ð¿Ð¾Ñ‚Ñ€ÐµÐ±ÑƒÑ” Ð°Ð´Ð¼Ñ–Ð½Ñ–ÑÑ‚Ñ€Ð°Ñ‚Ð¾Ñ€Ð°", currentUserId, requestName);
                return CreateUnauthorizedResult("ÐÐµÐ´Ð¾ÑÑ‚Ð°Ñ‚Ð½ÑŒÐ¾ Ð¿Ñ€Ð°Ð² Ð´Ð¾ÑÑ‚ÑƒÐ¿Ñƒ");
            }
        }
        // ÐŸÐµÑ€ÐµÐ²Ñ–Ñ€ÐºÐ° Ð²ÑÑ–Ñ… Ð´Ð¾Ð·Ð²Ð¾Ð»Ñ–Ð²
        else if (requireAllPermissionsAttr != null)
        {
            var hasAllPermissions = await _authorizationService.HasAllPermissionsAsync(
                currentUserId.Value, cancellationToken, requireAllPermissionsAttr.Permissions);
                
            if (!hasAllPermissions)
            {
                _logger.LogWarning(
                    "ÐšÐ¾Ñ€Ð¸ÑÑ‚ÑƒÐ²Ð°Ñ‡ {UserId} Ð½Ð°Ð¼Ð°Ð³Ð°Ð²ÑÑ Ð²Ð¸ÐºÐ¾Ð½Ð°Ñ‚Ð¸ ÐºÐ¾Ð¼Ð°Ð½Ð´Ñƒ {RequestName}, Ñ‰Ð¾ Ð¿Ð¾Ñ‚Ñ€ÐµÐ±ÑƒÑ” Ð´Ð¾Ð·Ð²Ð¾Ð»Ñ–Ð²: {Permissions}",
                    currentUserId, requestName, string.Join(", ", requireAllPermissionsAttr.Permissions));
                return CreateUnauthorizedResult("ÐÐµÐ´Ð¾ÑÑ‚Ð°Ñ‚Ð½ÑŒÐ¾ Ð¿Ñ€Ð°Ð² Ð´Ð¾ÑÑ‚ÑƒÐ¿Ñƒ");
            }
        }
        // ÐŸÐµÑ€ÐµÐ²Ñ–Ñ€ÐºÐ° ÐºÐ¾Ð½ÐºÑ€ÐµÑ‚Ð½Ð¾Ð³Ð¾ Ð´Ð¾Ð·Ð²Ð¾Ð»Ñƒ Ð°Ð±Ð¾ Ð°Ð»ÑŒÑ‚ÐµÑ€Ð½Ð°Ñ‚Ð¸Ð²
        else if (requirePermissionAttr != null)
        {
            var hasPermission = await _authorizationService.HasPermissionAsync(
                currentUserId.Value, requirePermissionAttr.Permission, cancellationToken);

            // Ð¯ÐºÑ‰Ð¾ Ð¾ÑÐ½Ð¾Ð²Ð½Ð¸Ð¹ Ð´Ð¾Ð·Ð²Ñ–Ð» Ð²Ñ–Ð´ÑÑƒÑ‚Ð½Ñ–Ð¹, Ð¿ÐµÑ€ÐµÐ²Ñ–Ñ€ÑÑ”Ð¼Ð¾ Ð°Ð»ÑŒÑ‚ÐµÑ€Ð½Ð°Ñ‚Ð¸Ð²Ð¸
            if (!hasPermission && requirePermissionAttr.AlternativePermissions != null)
            {
                hasPermission = await _authorizationService.HasAnyPermissionAsync(
                    currentUserId.Value, cancellationToken, requirePermissionAttr.AlternativePermissions);
            }

            if (!hasPermission)
            {
                var requiredPermissions = requirePermissionAttr.AlternativePermissions != null
                    ? $"{requirePermissionAttr.Permission} Ð°Ð±Ð¾ {string.Join(", ", requirePermissionAttr.AlternativePermissions)}"
                    : requirePermissionAttr.Permission.ToString();

                _logger.LogWarning(
                    "ÐšÐ¾Ñ€Ð¸ÑÑ‚ÑƒÐ²Ð°Ñ‡ {UserId} Ð½Ð°Ð¼Ð°Ð³Ð°Ð²ÑÑ Ð²Ð¸ÐºÐ¾Ð½Ð°Ñ‚Ð¸ ÐºÐ¾Ð¼Ð°Ð½Ð´Ñƒ {RequestName}, Ñ‰Ð¾ Ð¿Ð¾Ñ‚Ñ€ÐµÐ±ÑƒÑ” Ð´Ð¾Ð·Ð²Ð¾Ð»Ñ–Ð²: {Permissions}",
                    currentUserId, requestName, requiredPermissions);
                return CreateUnauthorizedResult("ÐÐµÐ´Ð¾ÑÑ‚Ð°Ñ‚Ð½ÑŒÐ¾ Ð¿Ñ€Ð°Ð² Ð´Ð¾ÑÑ‚ÑƒÐ¿Ñƒ");
            }
        }

        _logger.LogDebug("ÐÐ²Ñ‚Ð¾Ñ€Ð¸Ð·Ð°Ñ†Ñ–Ñ Ð¿Ñ€Ð¾Ð¹ÑˆÐ»Ð° ÑƒÑÐ¿Ñ–ÑˆÐ½Ð¾ Ð´Ð»Ñ ÐºÐ¾Ð¼Ð°Ð½Ð´Ð¸ {RequestName} ÐºÐ¾Ñ€Ð¸ÑÑ‚ÑƒÐ²Ð°Ñ‡Ð° {UserId}", requestName, currentUserId);

        return await next();
    }

    private static TResponse CreateUnauthorizedResult(string message)
    {
        // Ð¯ÐºÑ‰Ð¾ TResponse Ñ” Result<T>, ÑÑ‚Ð²Ð¾Ñ€Ð¸Ñ‚Ð¸ Fail result
        var responseType = typeof(TResponse);
        
        if (responseType.IsGenericType && responseType.GetGenericTypeDefinition() == typeof(Result<>))
        {
            var resultType = responseType.GetGenericArguments()[0];
            var failMethod = typeof(Result).GetMethod(nameof(Result.Fail), new[] { typeof(string) })?.MakeGenericMethod(resultType);
            
            if (failMethod != null)
            {
                var result = failMethod.Invoke(null, new object[] { message });
                return (TResponse)result!;
            }
        }
        
        // Ð¯ÐºÑ‰Ð¾ TResponse Ñ” Result, ÑÑ‚Ð²Ð¾Ñ€Ð¸Ñ‚Ð¸ Fail result
        if (responseType == typeof(Result))
        {
            var result = Result.Fail(message);
            return (TResponse)(object)result;
        }

        // Ð’ Ñ–Ð½ÑˆÐ¾Ð¼Ñƒ Ð²Ð¸Ð¿Ð°Ð´ÐºÑƒ ÐºÐ¸Ð½ÑƒÑ‚Ð¸ Ð²Ð¸Ð½ÑÑ‚Ð¾Ðº
        throw new UnauthorizedAccessException(message);
    }
}
