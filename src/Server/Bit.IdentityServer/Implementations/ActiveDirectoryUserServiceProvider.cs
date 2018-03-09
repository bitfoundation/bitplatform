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
        private string _activeDirectoryName;

        public virtual IAppEnvironmentProvider AppEnvironmentProvider
        {
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(AppEnvironmentProvider));
                _activeDirectoryName = value.GetActiveAppEnvironment().GetConfig<string>("ActiveDirectoryName");
            }
        }

        public override Task<string> GetUserIdByLocalAuthenticationContextAsync(LocalAuthenticationContext context)
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
                            return Task.FromResult(user.Sid.Value);
                    }
                }
            }

            throw new DomainLogicException("UserInActiveDirectoryCouldNotBeFound");
        }

        public override Task<bool> UserIsActiveAsync(IsActiveContext context, string userId)
        {
            using (PrincipalContext principalContext = new PrincipalContext(ContextType.Domain, _activeDirectoryName))
            {
                using (UserPrincipal user = UserPrincipal.FindByIdentity(principalContext, IdentityType.SamAccountName, context.Subject.Identity.Name))
                {
                    return Task.FromResult(user?.Enabled != null && user.Enabled.Value);
                }
            }
        }
    }
}