using Bit.IdentityServer.Implementations;
using Bit.Owin.Exceptions;
using IdentityServer3.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
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

        public override Task<string> GetUserIdByLocalAuthenticationContextAsync(LocalAuthenticationContext context, CancellationToken cancellationToken)
        {
            LocalUser user = _localUsers.SingleOrDefault(u => u.UserId == context.UserName && u.Password == context.Password);

            if (user == null)
                throw new DomainLogicException("LoginFailed");

            return Task.FromResult(user.UserId);
        }

        public override Task<bool> UserIsActiveAsync(IsActiveContext context, string userId, CancellationToken cancellationToken)
        {
            return Task.FromResult(_localUsers.Any(u => u.UserId == userId));
        }

        protected override Task<string> GetInternalUserId(ExternalAuthenticationContext context, CancellationToken cancellationToken)
        {
            string nameIdentifier = context.ExternalIdentity.Claims.GetClaimValue(ClaimTypes.NameIdentifier);

            if (nameIdentifier == null)
                throw new InvalidOperationException($"{nameof(nameIdentifier)} is null");

            LocalUser user = _localUsers.Find(u => u.UserId == nameIdentifier);

            if (user == null)
            {
                user = new LocalUser { UserId = nameIdentifier, Password = null };
                _localUsers.Add(user);
            }

            return Task.FromResult(user.UserId);
        }
    }
}
