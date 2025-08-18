using Bit.Core.Exceptions;
using Bit.Core.Models;
using Bit.IdentityServer.Contracts;
using Bit.IdentityServer.Implementations;
using System.Collections.Generic;
using System.Linq;
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
            if (context.AcrValues.TryGetValue("x", out string x))
                Logger.AddLogData("x", x);

            if (context.AcrValues.TryGetValue("y", out string y))
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
    }
}
