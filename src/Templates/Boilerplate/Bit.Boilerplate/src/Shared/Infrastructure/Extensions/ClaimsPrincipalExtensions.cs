//+:cnd:noEmit
namespace System.Security.Claims;

public static partial class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Claims a token refresh rewrites without changing anything the user is allowed to do: issued-at, not-before,
    /// expiry and the token's own id. Everything else the token carries - the user id, the roles, the features, the
    /// tenant, the session id, and the privileged/elevated markers - decides what the app may show and what the
    /// server will authorize, so all of it is significant.
    /// </summary>
    private static string[]? volatileClaimTypes;

    extension(ClaimsPrincipal? claimsPrincipal)
    {
        public bool IsAuthenticated()
        {
            return claimsPrincipal?.Identity?.IsAuthenticated is true;
        }

        /// <summary>
        /// True when two principals grant exactly the same thing - same identity, same roles, same features, same
        /// tenant, same session - ignoring only the claims a refresh rewrites for its own sake.
        /// <para>
        /// This is the question every <c>AuthenticationStateChanged</c> handler actually wants to ask. Comparing the
        /// user id alone is not enough: a tenant switch, an elevated-access grant and a role change all issue a new
        /// token for the SAME user, and a handler keyed on the user id treats them as "nothing happened".
        /// </para>
        /// </summary>
        public bool IsTheSame(ClaimsPrincipal? other)
        {
            if (ReferenceEquals(claimsPrincipal, other)) return true;

            if (claimsPrincipal is null || other is null) return false;

            if (claimsPrincipal.IsAuthenticated() != other.IsAuthenticated()) return false;

            volatileClaimTypes ??= ["exp", "iat", "nbf", "jti", "auth_time"];

            static Dictionary<(string Type, string Value), int> SignificantClaims(ClaimsPrincipal principal)
            {
                return principal.Claims
                                .Where(claim => volatileClaimTypes.Contains(claim.Type, StringComparer.OrdinalIgnoreCase) is false)
                                .GroupBy(claim => (claim.Type, claim.Value))
                                .ToDictionary(group => group.Key, group => group.Count());
            }

            var current = SignificantClaims(claimsPrincipal);
            var others = SignificantClaims(other);

            return current.Count == others.Count &&
                   current.All(claim => others.TryGetValue(claim.Key, out var count) && count == claim.Value);
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
