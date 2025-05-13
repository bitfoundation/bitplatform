namespace System.Security.Claims;

public static partial class ClaimsPrincipalExtensions
{
    public static bool IsAuthenticated(this ClaimsPrincipal? claimsPrincipal)
    {
        return claimsPrincipal?.Identity?.IsAuthenticated is true;
    }

    public static Guid GetUserId(this ClaimsPrincipal claimsPrincipal)
    {
        return Guid.Parse((claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier) ?? claimsPrincipal.FindFirst("nameid"))!.Value);
    }

    public static string GetUserName(this ClaimsPrincipal claimsPrincipal)
    {
        return (claimsPrincipal.FindFirst(ClaimTypes.Name) ?? claimsPrincipal.FindFirst("unique_name"))!.Value;
    }

    public static string? GetEmail(this ClaimsPrincipal claimsPrincipal)
    {
        return (claimsPrincipal.FindFirst(ClaimTypes.Email) ?? claimsPrincipal.FindFirst("email"))?.Value;
    }

    public static string GetDisplayName(this ClaimsPrincipal claimsPrincipal)
    {
        return claimsPrincipal.GetEmail() ?? claimsPrincipal.GetUserName();
    }

    /// <summary>
    /// Returns the user session id stored in sessions column of user table after user sign in.
    /// </summary>
    public static Guid GetSessionId(this ClaimsPrincipal claimsPrincipal)
    {
        return Guid.Parse(claimsPrincipal.FindFirst(AppClaimTypes.SESSION_ID)!.Value);
    }

    public static bool HasFeature(this ClaimsPrincipal claimsPrincipal, string feature)
    {
        return claimsPrincipal.HasClaim(AppClaimTypes.FEATURES, feature);
    }

    public static T? GetClaimValue<T>(this ClaimsPrincipal claimsPrincipal, string claimType)
    {
        var results = claimsPrincipal.FindAll(claimType).Select(c => c.Value).ToArray();

        if (results.Any() is false)
            return default!;

        try
        {
            Type targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
            return results.Select(r => (T)Convert.ChangeType(r, targetType, CultureInfo.InvariantCulture)!).Max(); // User might have multiple roles with this claim.
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to convert claim values for {claimType} to type {typeof(T).Name}.", ex);
        }
    }
}
