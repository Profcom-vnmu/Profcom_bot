using StudentUnionBot.Domain.Enums;

namespace StudentUnionBot.Application.Common.Attributes;

/// <summary>
/// Атрибут для позначення команд/запитів, що потребують авторизації
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class RequirePermissionAttribute : Attribute
{
    public Permission Permission { get; }
    public Permission[]? AlternativePermissions { get; }

    /// <summary>
    /// Команда/запит потребує конкретний дозвіл
    /// </summary>
    public RequirePermissionAttribute(Permission permission)
    {
        Permission = permission;
    }

    /// <summary>
    /// Команда/запит потребує один з альтернативних дозволів
    /// </summary>
    public RequirePermissionAttribute(Permission permission, params Permission[] alternativePermissions)
    {
        Permission = permission;
        AlternativePermissions = alternativePermissions;
    }
}

/// <summary>
/// Атрибут для позначення команд/запитів, що потребують всі зазначені дозволи
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class RequireAllPermissionsAttribute : Attribute
{
    public Permission[] Permissions { get; }

    public RequireAllPermissionsAttribute(params Permission[] permissions)
    {
        Permissions = permissions ?? Array.Empty<Permission>();
    }
}

/// <summary>
/// Атрибут для позначення команд/запитів, що потребують роль адміністратора
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class RequireAdminAttribute : Attribute
{
}

/// <summary>
/// Атрибут для позначення команд/запитів, що потребують роль суперадміністратора
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class RequireSuperAdminAttribute : Attribute
{
}