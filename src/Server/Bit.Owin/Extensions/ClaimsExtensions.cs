using System.Collections.Generic;
using System.Linq;

namespace System.Security.Claims
{
    public static class ClaimsExtensions
    {
        public static string GetClaimValue(this IEnumerable<Claim> claims, string claimType)
        {
            if (claims == null)
                throw new ArgumentNullException(nameof(claims));

            if (claimType == null)
                throw new ArgumentNullException(nameof(claimType));

            ClaimsIdentity subject = claims.FirstOrDefault()?.Subject;

            return subject?.FindFirst(claimType)?.Value ?? claims.ExtendedSingleOrDefault($"Finding claim: {claimType}", c => c.Type == claimType)?.Value;
        }
    }
}
