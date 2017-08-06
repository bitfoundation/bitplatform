using Bit.Core.Contracts;
using Bit.Owin.Exceptions;
using IdentityServer3.Core.Models;
using System;
using System.DirectoryServices.AccountManagement;
using System.Threading.Tasks;

namespace Bit.IdentityServer.Implementations
{
    public class ActiveDirectoryUserServiceProvider : UserService
    {
        private readonly string _activeDirectoryName;

        public ActiveDirectoryUserServiceProvider(IAppEnvironmentProvider appEnvironmentProvider)
        {
            if (appEnvironmentProvider == null)
                throw new ArgumentNullException(nameof(appEnvironmentProvider));

            _activeDirectoryName = appEnvironmentProvider.GetActiveAppEnvironment().GetConfig<string>("ActiveDirectoryName");
        }

#if DEBUG
        protected ActiveDirectoryUserServiceProvider()
        {
        }
#endif

        public override async Task<string> GetUserIdByLocalAuthenticationContextAsync(LocalAuthenticationContext context)
        {
            string username = context.UserName;
            string password = context.Password;

            using (PrincipalContext principalContext = new PrincipalContext(ContextType.Domain, _activeDirectoryName))
            {
                string userNameAsWinUserName = username;

                if (!userNameAsWinUserName.Contains(_activeDirectoryName))
                    userNameAsWinUserName = $"{_activeDirectoryName}\\{userNameAsWinUserName}";

                if (principalContext.ValidateCredentials(userNameAsWinUserName, password))
                {
                    using (UserPrincipal user = UserPrincipal.FindByIdentity(principalContext, IdentityType.SamAccountName, userNameAsWinUserName))
                    {
                        if (user != null)
                            return user.Sid.Value;
                    }
                }
            }

            throw new DomainLogicException("UserInActiveDirectoryCouldNotBeFound");
        }

        public override async Task<bool> UserIsActiveAsync(IsActiveContext context, string userId)
        {
            using (PrincipalContext principalContext = new PrincipalContext(ContextType.Domain, _activeDirectoryName))
            {
                using (UserPrincipal user = UserPrincipal.FindByIdentity(principalContext, IdentityType.SamAccountName, context.Subject.Identity.Name))
                {
                    return user?.Enabled != null && user.Enabled.Value;
                }
            }
        }
    }
}