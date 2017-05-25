using System;
using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;
using System.Security.Claims;
using System.Threading.Tasks;
using Bit.Core.Contracts;
using IdentityServer3.Core.Models;
using IdentityServer3.Core.Services.Default;

namespace Bit.IdentityServer.Implementations
{
    public class ActiveDirectoryUserServiceProvider : UserServiceBase
    {
        private readonly string _activeDirectoryName;

        public ActiveDirectoryUserServiceProvider(IAppEnvironmentProvider appEnvironmentProvider)
        {
            if (appEnvironmentProvider == null)
                throw new ArgumentNullException(nameof(appEnvironmentProvider));

            _activeDirectoryName = appEnvironmentProvider.GetActiveAppEnvironment().GetConfig<string>("ActiveDirectoryName");
        }

        protected ActiveDirectoryUserServiceProvider()
        {

        }

        public override async Task AuthenticateLocalAsync(LocalAuthenticationContext context)
        {
            string username = context.UserName;
            string password = context.Password;

            try
            {
                using (PrincipalContext principalContext = new PrincipalContext(ContextType.Domain, _activeDirectoryName))
                {
                    string userNameAsWinUserName = username;

                    if (!userNameAsWinUserName.Contains(_activeDirectoryName))
                        userNameAsWinUserName = $"{_activeDirectoryName}\\{userNameAsWinUserName}";

                    if (principalContext.ValidateCredentials(userNameAsWinUserName, password))
                    {
                        using (
                            UserPrincipal user = UserPrincipal.FindByIdentity(principalContext,
                                IdentityType.SamAccountName, userNameAsWinUserName))
                        {
                            if (user == null)
                                throw new InvalidOperationException("LoginFailed");

                            List<Claim> claims = new List<Claim>
                            {
                                new Claim("sub", user.Sid.Value),
                                new Claim("primary_sid", user.Sid.Value),
                                new Claim("upn", user.UserPrincipalName),
                                new Claim("name", user.Name),
                                new Claim("given_name", user.GivenName)
                            };

                            AuthenticateResult result = new AuthenticateResult(user.SamAccountName, user.SamAccountName,
                                claims,
                                authenticationMethod: "custom");

                            context.AuthenticateResult = result;
                        }
                    }
                    else
                    {
                        throw new InvalidOperationException("LoginFailed");
                    }
                }

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
            using (PrincipalContext principalContext = new PrincipalContext(ContextType.Domain, _activeDirectoryName))
            {
                using (
                    UserPrincipal user = UserPrincipal.FindByIdentity(principalContext, IdentityType.SamAccountName,
                        context.Subject.Identity.Name))
                {
                    if (user != null)
                    {
                        List<Claim> claims = new List<Claim>
                        {
                            new Claim("sub", user.Sid.Value),
                            new Claim("primary_sid", user.Sid.Value),
                            new Claim("upn", user.UserPrincipalName),
                            new Claim("name", user.Name),
                            new Claim("given_name", user.GivenName)
                        };

                        context.IssuedClaims = claims;

                        await base.GetProfileDataAsync(context);
                    }
                }
            }
        }

        public override async Task IsActiveAsync(IsActiveContext context)
        {
            context.IsActive = false;

            using (PrincipalContext principalContext = new PrincipalContext(ContextType.Domain, _activeDirectoryName))
            {
                using (
                    UserPrincipal user = UserPrincipal.FindByIdentity(principalContext, IdentityType.SamAccountName,
                        context.Subject.Identity.Name))
                {
                    if (user != null)
                    {
                        context.IsActive = user.Enabled != null && user.Enabled.Value;
                    }
                }
            }

            await base.IsActiveAsync(context);
        }
    }
}