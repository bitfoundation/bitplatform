using Bit.IdentityServer.Implementations;
using Bit.Owin.Exceptions;
using IdentityServer3.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Bit.Tests.IdentityServer.Implementations
{
    public class LocalUser
    {
        public virtual string UserId { get; set; }
        public virtual string Password { get; set; }
    }

    public class TestUserService : DefaultUserService
    {
        private readonly List<LocalUser> _localUsers = new List<LocalUser>
        {
            new LocalUser { UserId = "SomeOne" , Password = "ValidPassword" },
            new LocalUser { UserId = "ValidUserName" , Password = "ValidPassword" },
            new LocalUser { UserId = "User2" , Password = "ValidPassword"}
        };

        public override async Task<string> GetUserIdByLocalAuthenticationContextAsync(LocalAuthenticationContext context)
        {
            LocalUser user = _localUsers.SingleOrDefault(u => u.UserId == context.UserName && u.Password == context.Password);

            if (user == null)
                throw new DomainLogicException("LoginFailed");

            return user.UserId;
        }

        public override async Task<bool> UserIsActiveAsync(IsActiveContext context, string userId)
        {
            return _localUsers.Any(u => u.UserId == userId);
        }
    }
}
