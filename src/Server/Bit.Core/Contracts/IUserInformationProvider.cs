using System.Collections.Generic;
using System.Security.Claims;

namespace Bit.Core.Contracts
{
    public interface IUserInformationProvider
    {
        bool IsAuthenticated();

        string GetCurrentUserId();

        string GetAuthenticationType();

        string GetClientId();

        IEnumerable<Claim> GetClaims();

        ClaimsIdentity GetIdentity();
    }
}
