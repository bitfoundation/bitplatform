using Bit.Core.Contracts;
using Bit.Core.Models;
using Bit.Owin.Exceptions;
using IdentityServer3.Core.Models;
using System;
using System.DirectoryServices.AccountManagement;
using System.Threading.Tasks;

namespace Bit.IdentityServer.Implementations
{
    public class ActiveDirectoryUserServiceProvider : UserService
    {
        public virtual AppEnvironment AppEnvironment { get; set; }

        public override Task<string> GetUserIdByLocalAuthenticationContextAsync(LocalAuthenticationContext context)
        {
            string username = context.UserName;
            string password = context.Password;
            string activeDirectoryName = AppEnvironment.GetConfig<string>("ActiveDirectoryName");
            using (PrincipalContext principalContext = new PrincipalContext(ContextType.Domain, activeDirectoryName))
            {
                string userNameAsWinUserName = username;

                if (!userNameAsWinUserName.Contains(activeDirectoryName))
                    userNameAsWinUserName = $"{activeDirectoryName}\\{userNameAsWinUserName}";

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
            string activeDirectoryName = AppEnvironment.GetConfig<string>("ActiveDirectoryName");

            using (PrincipalContext principalContext = new PrincipalContext(ContextType.Domain, activeDirectoryName))
            {
                using (UserPrincipal user = UserPrincipal.FindByIdentity(principalContext, IdentityType.SamAccountName, context.Subject.Identity.Name))
                {
                    return Task.FromResult(user?.Enabled != null && user.Enabled.Value);
                }
            }
        }
    }
}