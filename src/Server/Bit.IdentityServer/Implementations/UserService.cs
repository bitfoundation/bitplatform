using Bit.Owin.Contracts;
using IdentityServer3.Core.Models;
using IdentityServer3.Core.Services.Default;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Bit.IdentityServer.Implementations
{
    public abstract class UserService : UserServiceBase
    {
        public abstract Task<string> GetUserIdByLocalAuthenticationContextAsync(LocalAuthenticationContext context);

        public virtual IExceptionToHttpErrorMapper ExceptionToHttpErrorMapper { get; set; }

        public override async Task AuthenticateLocalAsync(LocalAuthenticationContext context)
        {
            try
            {
                string userId = await GetUserIdByLocalAuthenticationContextAsync(context).ConfigureAwait(false);

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

            await base.AuthenticateLocalAsync(context).ConfigureAwait(false);
        }

        public sealed override async Task GetProfileDataAsync(ProfileDataRequestContext context)
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

            await base.GetProfileDataAsync(context).ConfigureAwait(false);
        }

        public abstract Task<bool> UserIsActiveAsync(IsActiveContext context, string userId);

        public override async Task IsActiveAsync(IsActiveContext context)
        {
            try
            {
                context.IsActive = await UserIsActiveAsync(context, context.Subject.Identity.Name).ConfigureAwait(false);
            }
            catch
            {
                context.IsActive = false;
            }
            finally
            {
                context.IsActive = true; // Temporary fix: To prevent redirect loop on logout.
            }

            await base.IsActiveAsync(context).ConfigureAwait(false);
        }

        protected virtual async Task<string> GetInternalUserId(ExternalAuthenticationContext context)
        {
            throw new NotImplementedException();
        }

        public override async Task AuthenticateExternalAsync(ExternalAuthenticationContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (context.AuthenticateResult == null)
            {
                string userId = await GetInternalUserId(context).ConfigureAwait(false);

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
                    authenticationMethod: context.ExternalIdentity.Provider);

                context.AuthenticateResult = result;
            }

            await base.AuthenticateExternalAsync(context).ConfigureAwait(false);
        }
    }
}
