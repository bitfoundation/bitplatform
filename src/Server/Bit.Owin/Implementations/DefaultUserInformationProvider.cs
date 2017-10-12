using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Bit.Core.Contracts;

namespace Bit.Owin.Implementations
{
    public class DefaultUserInformationProvider : IUserInformationProvider
    {
        public virtual IRequestInformationProvider RequestInformationProvider { get; set; }

        public virtual bool IsAuthenticated()
        {
            ClaimsIdentity identity = GetIdentity();

            if (identity == null)
                return false;

            return identity.IsAuthenticated;
        }

        public virtual string GetCurrentUserId()
        {
            return GetClaims()
                .ExtendedSingle("Finding primary_sid in claims", claim => string.Equals(claim.Type, "primary_sid", StringComparison.OrdinalIgnoreCase))
                .Value;
        }

        public virtual string GetAuthenticationType()
        {
            ClaimsIdentity claimsIdentity = GetIdentity();

            if (claimsIdentity == null)
                throw new InvalidOperationException("Principal identity is not ClaimsIdentity or user is not authenticated");

            return claimsIdentity.AuthenticationType;
        }

        public virtual IEnumerable<Claim> GetClaims()
        {
            ClaimsIdentity claimsIdentity = GetIdentity();

            if (claimsIdentity == null)
                throw new InvalidOperationException("Principal identity is not ClaimsIdentity or user is not authenticated");

            return claimsIdentity.Claims;
        }

        public virtual ClaimsIdentity GetIdentity()
        {
            return RequestInformationProvider.Identity;
        }

        public virtual string GetClientId()
        {
            return GetClaims()
                .ExtendedSingle("Finding client_id in claims", claim => string.Equals(claim.Type, "client_id", StringComparison.OrdinalIgnoreCase))
                .Value;
        }
    }
}