using Bit.Core.Contracts;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

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
            return GetBitJwtToken().UserId;
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

        public virtual BitJwtToken GetBitJwtToken()
        {
            string primary_sid = GetClaims()
                .ExtendedSingle("Finding primary_sid in claims", claim => string.Equals(claim.Type, "primary_sid", StringComparison.OrdinalIgnoreCase))
                .Value;

            try
            {
                JToken json = JToken.Parse(primary_sid);
                return new BitJwtToken { UserId = (json[nameof(BitJwtToken.UserId)] ?? throw new InvalidOperationException("UserId_Could_Not_Be_Found_In_Bit_Jwt_Token")).ToObject<string>(), CustomProps = (json[nameof(BitJwtToken.CustomProps)] ?? throw new InvalidOperationException("CustomProps_Could_Not_Be_Found_In_Bit_Jwt_Token")).ToObject<Dictionary<string, string>>() };
            }
            catch (InvalidOperationException exp) when (exp.Message.EndsWith("_Could_Not_Be_Found_In_Bit_Jwt_Token"))
            {
                return new BitJwtToken { UserId = primary_sid };
            }
        }
    }
}