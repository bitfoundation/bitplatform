﻿using Bit.Core.Models;
using System.Collections.Generic;
using System.Security.Claims;

namespace Bit.Core.Contracts
{
    public interface IUserInformationProvider
    {
        bool IsAuthenticated();

        string? GetCurrentUserId();

        string GetAuthenticationType();

        IEnumerable<Claim> GetClaims();

        ClaimsIdentity? GetIdentity();

        BitJwtToken GetBitJwtToken();
    }
}
