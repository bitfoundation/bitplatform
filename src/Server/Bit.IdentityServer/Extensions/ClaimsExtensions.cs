using System;
using System.Collections.Generic;
using System.Linq;

namespace IdentityServer3.Core.Models
{
    public static class ClaimsExtensions
    {
        public static string GetClaimValue(this IEnumerable<System.Security.Claims.Claim> claims, string claimType)
        {
            if (claims == null)
                throw new ArgumentNullException(nameof(claims));

            if (claimType == null)
                throw new ArgumentNullException(nameof(claimType));

            System.Security.Claims.ClaimsIdentity subject = claims.FirstOrDefault()?.Subject;

            return subject?.FindFirst(claimType)?.Value ?? claims.ExtendedSingleOrDefault($"Finding claim: {claimType}", c => c.Type == claimType)?.Value;
        }
    }
}
