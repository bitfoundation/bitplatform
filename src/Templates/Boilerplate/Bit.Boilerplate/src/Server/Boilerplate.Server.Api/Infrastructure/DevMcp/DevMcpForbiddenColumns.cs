using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;

namespace Boilerplate.Server.Api.Infrastructure.DevMcp;

public static class DevMcpForbiddenColumns
{
    private static readonly HashSet<string> ForbiddenEntities = new(StringComparer.OrdinalIgnoreCase)
    {
        nameof(UserToken), nameof(WebAuthnCredential), nameof(DataProtectionKey)
    };

    private static readonly Dictionary<string, HashSet<string>> ForbiddenProperties = new(StringComparer.OrdinalIgnoreCase)
    {
        [nameof(User)] = new(StringComparer.OrdinalIgnoreCase)
        {
            nameof(User.PasswordHash), nameof(User.SecurityStamp), nameof(User.ConcurrencyStamp)
        },
        [nameof(UserLogin)] = new(StringComparer.OrdinalIgnoreCase)
        {
            nameof(UserLogin.ProviderKey)
        }
    };

    private static readonly string[] CredentialFragments =
    [
        "PasswordHash", "Password", "SecurityStamp", "ConcurrencyStamp",
        "Authenticator", "RecoveryCode", "PrivateKey", "ApiKey", "ClientSecret",
        "ConsumerSecret", "SecretKey", "ConnectionString"
    ];

    public static string? RejectEntity(IEntityType entityType)
    {
        if (ForbiddenEntities.Contains(entityType.ClrType.Name))
            return $"Entity '{entityType.ClrType.Name}' is credential-shaped and cannot be queried.";

        return null;
    }

    public static string? RejectProperty(IEntityType root, string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return "A property name is required.";

        IEntityType? current = root;
        string? navigationEntityName = root.ClrType.Name;

        var segments = path.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (segments.Length == 0)
            return "A property name is required.";

        for (var i = 0; i < segments.Length; i++)
        {
            var segment = segments[i];
            var isLast = i == segments.Length - 1;

            if (LooksLikeCredential(navigationEntityName!, segment))
                return $"Property '{path}' is forbidden. Credential-shaped columns are rejected at input.";

            if (current is null)
                return $"Unknown property '{path}' on {root.ClrType.Name}.";

            var property = current.FindProperty(segment);
            if (property is not null)
            {
                if (isLast is false)
                    return $"'{segment}' is a scalar and cannot be navigated through in '{path}'.";
                return null;
            }

            INavigationBase? navigation = current.FindNavigation(segment);
            navigation ??= current.FindSkipNavigation(segment);
            if (navigation is null)
                return $"Unknown property '{path}' on {root.ClrType.Name}.";

            if (isLast)
                return "Projections and ordering accept scalar columns only, not navigations.";

            current = navigation.TargetEntityType;
            navigationEntityName = current.ClrType.Name;

            var entityError = RejectEntity(current);
            if (entityError is not null)
                return entityError;
        }

        return null;
    }

    public static bool IsHangfireStorage(IEntityType entityType)
        => string.Equals(entityType.GetSchema(), "jobs", StringComparison.OrdinalIgnoreCase)
           || entityType.ClrType.Namespace?.StartsWith("Hangfire", StringComparison.Ordinal) is true;

    public static bool LooksLikeCredential(string entityName, string propertyName)
    {
        if (ForbiddenEntities.Contains(entityName))
            return true;

        if (ForbiddenProperties.TryGetValue(entityName, out var properties) && properties.Contains(propertyName))
            return true;

        if (propertyName.Contains("Token", StringComparison.OrdinalIgnoreCase)
            && propertyName.EndsWith("RequestedOn", StringComparison.OrdinalIgnoreCase) is false
            && propertyName.EndsWith("Lifetime", StringComparison.OrdinalIgnoreCase) is false)
            return true;

        return CredentialFragments.Any(fragment => propertyName.Contains(fragment, StringComparison.OrdinalIgnoreCase));
    }
}
