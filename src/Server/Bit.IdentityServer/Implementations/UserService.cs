using Bit.Core.Contracts;
using Bit.Core.Implementations;
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

        public virtual Task<BitJwtToken> LocalLogin(LocalAuthenticationContext context, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public virtual IExceptionToHttpErrorMapper ExceptionToHttpErrorMapper { get; set; }

        public sealed override Task AuthenticateLocalAsync(LocalAuthenticationContext context)
        {
            return AuthenticateLocalAsync(context, OwinContext.Request.CallCancelled);
        }

        protected virtual List<Claim> BuildClaimsFromBitJwtToken(BitJwtToken bitJwtToken)
        {
            Claim primary_sid = new Claim("primary_sid", BitJwtToken.ToJson(bitJwtToken));

            List<Claim> claims = new List<Claim>
            {
                primary_sid
            };

            return claims;
        }

        public virtual async Task AuthenticateLocalAsync(LocalAuthenticationContext context, CancellationToken cancellationToken)
        {
            try
            {
                BitJwtToken bitJwtToken = await LocalLogin(context, cancellationToken).ConfigureAwait(false);

                if (context.AuthenticateResult == null)
                {
                    AuthenticateResult result = new AuthenticateResult(bitJwtToken.UserId, bitJwtToken.UserId,
                        BuildClaimsFromBitJwtToken(bitJwtToken),
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
            BitJwtToken bitJwtToken = BitJwtToken.FromJson(context.Subject.Claims.GetClaimValue("primary_sid"));

            context.IssuedClaims = BuildClaimsFromBitJwtToken(bitJwtToken);

            return base.GetProfileDataAsync(context);
        }

        public virtual Task<bool> UserIsActiveAsync(IsActiveContext context, BitJwtToken jwtToken, CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }

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
                    context.IsActive = await UserIsActiveAsync(context, UserInformationProvider.GetBitJwtToken(), OwinContext.Request.CallCancelled).ConfigureAwait(false);
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

        protected virtual Task<BitJwtToken> ExternalLogin(ExternalAuthenticationContext context, CancellationToken cancellationToken)
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
                BitJwtToken jwtToken = await ExternalLogin(context, cancellationToken).ConfigureAwait(false);

                AuthenticateResult result = new AuthenticateResult(jwtToken.UserId, jwtToken.UserId,
                    BuildClaimsFromBitJwtToken(jwtToken),
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