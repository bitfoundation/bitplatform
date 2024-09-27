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

    public static Guid? GetSessionId(this ClaimsPrincipal claimsPrincipal)
    {
        return claimsPrincipal.IsAuthenticated()
            ? Guid.Parse(claimsPrincipal.FindFirst("session-id")!.Value) 
            : null;
    }
}
