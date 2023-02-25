namespace System.Security.Claims;

public static class ClaimsPrincipalExtensions
{
    public static int GetUserId(this ClaimsPrincipal claimsPrincipal)
    {
        return int.Parse(claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)!.Value);
    }

    public static string GetUserName(this ClaimsPrincipal claimsPrincipal)
    {
        return claimsPrincipal.FindFirst(ClaimTypes.Name)!.Value;
    }
}
