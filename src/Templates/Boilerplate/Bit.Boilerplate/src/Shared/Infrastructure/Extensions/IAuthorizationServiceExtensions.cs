namespace Microsoft.AspNetCore.Authorization;

public static class IAuthorizationServiceExtensions
{
    extension(IAuthorizationService authorizationService)
    {
        public async Task<bool> IsAuthorized(ClaimsPrincipal user, string policyName)
        {
            var result = await authorizationService.AuthorizeAsync(user, policyName);
            return result.Succeeded;
        }
    }
}
