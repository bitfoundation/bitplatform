using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using IdentityServer3.Core.Models;
using IdentityServer3.Core.Services.Default;

namespace Bit.Tests.Api.Implementations
{
    public class LocalUser
    {
        public virtual string UserId { get; set; }
        public virtual string Password { get; set; }
    }

    public class TestUserService : UserServiceBase
    {
        private readonly List<LocalUser> _localUsers = new List<LocalUser>
        {
            new LocalUser { UserId = "ValidUserName" , Password = "ValidPassword" }
        };

        public override async Task AuthenticateLocalAsync(LocalAuthenticationContext context)
        {
            string username = context.UserName;

            LocalUser user = _localUsers.SingleOrDefault(u => u.UserId == username && u.Password == context.Password);

            try
            {
                if (user == null)
                    throw new InvalidOperationException("LoginFailed");

                List<Claim> claims = new List<Claim>
                {
                    new Claim("sub",user.UserId),
                    new Claim("primary_sid", user.UserId),
                    new Claim("upn", user.UserId),
                    new Claim("name", user.UserId),
                    new Claim("given_name", user.UserId)
                };

                AuthenticateResult result = new AuthenticateResult(user.UserId, user.UserId,
                    claims,
                    authenticationMethod: "custom");

                context.AuthenticateResult = result;

                await base.AuthenticateLocalAsync(context);
            }
            catch
            {
                AuthenticateResult result = new AuthenticateResult("LoginFailed");

                context.AuthenticateResult = result;
            }
        }

        public override async Task GetProfileDataAsync(ProfileDataRequestContext context)
        {
            LocalUser user = _localUsers.SingleOrDefault(u => u.UserId == context.Subject.Identity.Name);

            if (user != null)
            {
                List<Claim> claims = new List<Claim>
                {
                    new Claim("sub",user.UserId),
                    new Claim("primary_sid", user.UserId),
                    new Claim("upn", user.UserId),
                    new Claim("name", user.UserId),
                    new Claim("given_name", user.UserId)
                };

                context.IssuedClaims = claims;

                await base.GetProfileDataAsync(context);
            }
        }

        public override async Task IsActiveAsync(IsActiveContext context)
        {
            context.IsActive = false;

            LocalUser user = _localUsers.SingleOrDefault(u => u.UserId == context.Subject.Identity.Name);

            if (user != null)
            {
                context.IsActive = true;
            }

            await base.IsActiveAsync(context);
        }
    }
}
