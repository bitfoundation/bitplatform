using System;
using System.Security.Claims;
using Foundation.Core.Contracts;
using System.Linq;
using System.Collections.Generic;

namespace Foundation.Api.Implementations
{
    public class DefaultUserInformationProvider : IUserInformationProvider
    {
        private readonly IRequestInformationProvider _requestInformationProvider;

        protected DefaultUserInformationProvider()
        {
            
        }

        public DefaultUserInformationProvider(IRequestInformationProvider requestInformationProvider)
        {
            if (requestInformationProvider == null)
                throw new ArgumentNullException(nameof(requestInformationProvider));

            _requestInformationProvider = requestInformationProvider;
        }

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
                .Single(claim => string.Equals(claim.Type, "primary_sid", StringComparison.OrdinalIgnoreCase))
                .Value; ;
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
            return _requestInformationProvider.Identity;
        }
    }
}