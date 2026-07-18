//+:cnd:noEmit
namespace System.Security.Claims;

public static partial class ClaimsPrincipalExtensions
{
    extension(ClaimsPrincipal? claimsPrincipal)
    {
        public bool IsAuthenticated()
        {
            return claimsPrincipal?.Identity?.IsAuthenticated is true;
        }
    }

    extension(ClaimsPrincipal claimsPrincipal)
    {
        public Guid GetUserId()
        {
            return Guid.Parse((claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier) ?? claimsPrincipal.FindFirst("nameid"))!.Value);
        }

        public string GetUserName()
        {
            return (claimsPrincipal.FindFirst(ClaimTypes.Name) ?? claimsPrincipal.FindFirst("unique_name"))!.Value;
        }

        public string? GetEmail()
        {
            return (claimsPrincipal.FindFirst(ClaimTypes.Email) ?? claimsPrincipal.FindFirst("email"))?.Value;
        }

        public string GetDisplayName()
        {
            return claimsPrincipal.GetEmail() ?? claimsPrincipal.GetUserName();
        }

        /// <summary>
        /// Returns the user session id stored in sessions column of user table after user sign in.
        /// </summary>
        public Guid GetSessionId()
        {
            return Guid.Parse(claimsPrincipal.FindFirst(AppClaimTypes.SESSION_ID)!.Value);
        }

        //#if (multitenant == true)
        /// <summary>
        /// Returns the id of the tenant the user is currently signed into, or null if the user doesn't belong to any tenant yet.
        /// </summary>
        public Guid? GetTenantId()
        {
            var tenantId = claimsPrincipal.FindFirst(AppClaimTypes.TENANT_ID)?.Value;

            return string.IsNullOrEmpty(tenantId) ? null : Guid.Parse(tenantId);
        }
        //#endif

        public bool HasFeature(string feature)
        {
            return claimsPrincipal.HasClaim(AppClaimTypes.FEATURES, feature);
        }

        /// <summary>
        /// Returns the moment until which the session stays elevated, or null if the session was never elevated.
        /// <see cref="AuthPolicies.ELEVATED_ACCESS"/>
        /// </summary>
        public DateTimeOffset? GetElevatedSessionExpiresOn()
        {
            var value = claimsPrincipal.FindFirst(AppClaimTypes.ELEVATED_SESSION)?.Value;

            return string.IsNullOrEmpty(value) ? null : DateTimeOffset.FromUnixTimeSeconds(long.Parse(value, CultureInfo.InvariantCulture));
        }

        public T? GetClaimValue<T>(string claimType)
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
}
