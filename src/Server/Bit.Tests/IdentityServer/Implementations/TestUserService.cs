using Bit.Core.Contracts;
using Bit.Core.Exceptions;
using Bit.Core.Models;
using Bit.Http.Implementations;
using Bit.IdentityServer.Implementations;
using IdentityServer3.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Bit.Tests.IdentityServer.Implementations
{
    public class LocalUser
    {
        public virtual string UserId { get; set; }
        public virtual string Password { get; set; }
    }

    public class TestUserService : UserService
    {
        private readonly List<LocalUser> _localUsers = new List<LocalUser>
        {
            new LocalUser { UserId = "SomeOne" , Password = "ValidPassword" },
            new LocalUser { UserId = "ValidUserName" , Password = "ValidPassword" },
            new LocalUser { UserId = "User2" , Password = "ValidPassword"}
        };

        public async override Task<BitJwtToken> LocalLogin(LocalAuthenticationContext context, CancellationToken cancellationToken)
        {
            if (context.SignInMessage.TryGetValueFromAcr("x", out string x))
                Logger.AddLogData("x", x);

            if (context.SignInMessage.TryGetValueFromAcr("y", out string y))
                Logger.AddLogData("y", y);

            Logger.AddLogData("username", context.UserName);
            Logger.AddLogData("password", context.Password);

            LocalUser user = _localUsers.SingleOrDefault(u => u.UserId == context.UserName && u.Password == context.Password);

            if (user == null)
                throw new DomainLogicException("LoginFailed");

            BitJwtToken jwtToken = new BitJwtToken
            {
                UserId = user.UserId,
                Claims = new Dictionary<string, string?>
                {
                    { "custom-data", "test" }
                }
            };
            return jwtToken;
        }

        public override async Task<bool> UserIsActiveAsync(IsActiveContext context, BitJwtToken bitJwtToken, CancellationToken cancellationToken)
        {
            return _localUsers.Any(u => u.UserId == bitJwtToken.UserId);
        }

        protected override async Task<BitJwtToken> ExternalLogin(ExternalAuthenticationContext context, CancellationToken cancellationToken)
        {
            string nameIdentifier = context.ExternalIdentity.Claims.GetClaimValue(ClaimTypes.NameIdentifier);

            if (nameIdentifier == null)
                throw new InvalidOperationException($"{nameof(nameIdentifier)} is null");

            LocalUser user = _localUsers.FirstOrDefault(u => u.UserId == nameIdentifier);

            if (user == null)
            {
                user = new LocalUser { UserId = nameIdentifier, Password = null };
                _localUsers.Add(user);
            }

            return new BitJwtToken { UserId = user.UserId };
        }
    }
}
