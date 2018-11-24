using Bit.Core.Contracts;
using Bit.Owin.Contracts;
using IdentityServer3.Core.Models;
using IdentityServer3.Core.Services.Default;
using Microsoft.Owin;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace Bit.IdentityServer.Implementations
{
    public abstract class UserService : UserServiceBase
    {
        public virtual IOwinContext OwinContext { get; set; }

        public virtual IUserInformationProvider UserInformationProvider { get; set; }

        public abstract Task<string> GetUserIdByLocalAuthenticationContextAsync(LocalAuthenticationContext context, CancellationToken cancellationToken);

        public virtual IExceptionToHttpErrorMapper ExceptionToHttpErrorMapper { get; set; }

        public sealed override Task AuthenticateLocalAsync(LocalAuthenticationContext context)
        {
            return AuthenticateLocalAsync(context, OwinContext.Request.CallCancelled);
        }

        public virtual async Task AuthenticateLocalAsync(LocalAuthenticationContext context, CancellationToken cancellationToken)
        {
            try
            {
                string userId = await GetUserIdByLocalAuthenticationContextAsync(context, cancellationToken).ConfigureAwait(false);

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

        public sealed override Task GetProfileDataAsync(ProfileDataRequestContext context)
        {
            return GetProfileDataAsync(context, OwinContext.Request.CallCancelled);
        }

        public virtual Task GetProfileDataAsync(ProfileDataRequestContext context, CancellationToken cancellationToken)
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

            return base.GetProfileDataAsync(context);
        }

        public abstract Task<bool> UserIsActiveAsync(IsActiveContext context, string userId, CancellationToken cancellationToken);

        public sealed override async Task IsActiveAsync(IsActiveContext context)
        {
            try
            {
                if (UserInformationProvider.IsAuthenticated() == false)
                {
                    context.IsActive = false;
                }
                else
                {
                    context.IsActive = await UserIsActiveAsync(context, UserInformationProvider.GetCurrentUserId(), OwinContext.Request.CallCancelled).ConfigureAwait(false);
                }
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

        protected virtual Task<string> GetInternalUserId(ExternalAuthenticationContext context, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public sealed override Task AuthenticateExternalAsync(ExternalAuthenticationContext context)
        {
            return AuthenticateExternalAsync(context, OwinContext.Request.CallCancelled);
        }

        public virtual async Task AuthenticateExternalAsync(ExternalAuthenticationContext context, CancellationToken cancellationToken)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (context.AuthenticateResult == null)
            {
                string userId = await GetInternalUserId(context, cancellationToken).ConfigureAwait(false);

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

        public sealed override Task PostAuthenticateAsync(PostAuthenticationContext context)
        {
            return PostAuthenticateAsync(context, OwinContext.Request.CallCancelled);
        }

        public sealed override Task PreAuthenticateAsync(PreAuthenticationContext context)
        {
            return PreAuthenticateAsync(context, OwinContext.Request.CallCancelled);
        }

        public sealed override Task SignOutAsync(SignOutContext context)
        {
            return SignOutAsync(context, OwinContext.Request.CallCancelled);
        }

        public virtual Task PostAuthenticateAsync(PostAuthenticationContext context, CancellationToken cancellationToken)
        {
            return base.PostAuthenticateAsync(context);
        }

        public virtual Task PreAuthenticateAsync(PreAuthenticationContext context, CancellationToken cancellationToken)
        {
            return base.PreAuthenticateAsync(context);
        }

        public virtual Task SignOutAsync(SignOutContext context, CancellationToken cancellationToken)
        {
            return base.SignOutAsync(context);
        }
    }
}
