namespace Microsoft.AspNetCore.Authorization;

public static class IAuthorizationServiceExtensions
{
    public static async Task<bool> IsAuthorizedAsync(this IAuthorizationService authorizationService, ClaimsPrincipal user, string policyName)
    {
        var result = await authorizationService.AuthorizeAsync(user, policyName);
        return result.Succeeded;
    }
}
