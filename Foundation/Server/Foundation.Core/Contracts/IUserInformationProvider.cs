using System.Collections.Generic;
using System.Security.Claims;

namespace Foundation.Core.Contracts
{
    public interface IUserInformationProvider
    {
        bool IsAuthenticated();

        string GetCurrentUserId();

        string GetAuthenticationType();

        IEnumerable<Claim> GetClaims();

        ClaimsIdentity GetIdentity();
    }
}