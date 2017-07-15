using Bit.Owin.Contracts;
using IdentityServer3.Core.Models;
using IdentityServer3.Core.Services.Default;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Bit.IdentityServer.Implementations
{
    public abstract class DefaultUserService : UserServiceBase
    {
        public abstract Task<string> GetUserIdByLocalAuthenticationContextAsync(LocalAuthenticationContext context);

        public virtual IExceptionToHttpErrorMapper ExceptionToHttpErrorMapper { get; set; }

        public override async Task AuthenticateLocalAsync(LocalAuthenticationContext context)
        {
            try
            {
                string userId = await GetUserIdByLocalAuthenticationContextAsync(context);

                if (context.AuthenticateResult == null)
                {
                    List<Claim> claims = new List<Claim>
                    {
                        new Claim("sub", userId),
                        new Claim("primary_sid", userId),
                        new Claim("upn", userId),
                        new Claim("name", userId),
                        new Claim("given_name", userId)
                    };

                    AuthenticateResult result = new AuthenticateResult(userId, userId,
                        claims,
                        authenticationMethod: "custom");

                    context.AuthenticateResult = result;
                }
            }
            catch (Exception ex)
            {
                if (context.AuthenticateResult == null && ExceptionToHttpErrorMapper.IsKnownError(ex))
                    context.AuthenticateResult = new AuthenticateResult(ExceptionToHttpErrorMapper.GetMessage(ex));
                else
                    throw;
            }

            await base.AuthenticateLocalAsync(context);
        }

        public override async sealed Task GetProfileDataAsync(ProfileDataRequestContext context)
        {
            string userId = context.Subject.Identity.Name;

            List<Claim> claims = new List<Claim>
            {
                new Claim("sub", userId),
                new Claim("primary_sid", userId),
                new Claim("upn", userId),
                new Claim("name", userId),
                new Claim("given_name", userId)
            };

            context.IssuedClaims = claims;

            await base.GetProfileDataAsync(context);
        }

        public abstract Task<bool> UserIsActiveAsync(IsActiveContext context, string userId);

        public override async Task IsActiveAsync(IsActiveContext context)
        {
            try
            {
                context.IsActive = await UserIsActiveAsync(context, context.Subject.Identity.Name);
            }
            catch
            {
                context.IsActive = false;
            }
            finally
            {
                context.IsActive = true; // Temporary fix: To prevent redirect loop on logout.
            }

            await base.IsActiveAsync(context);
        }
    }
}
