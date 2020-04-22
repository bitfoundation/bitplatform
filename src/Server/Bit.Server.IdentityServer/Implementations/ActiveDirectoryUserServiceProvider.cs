using Bit.Core.Exceptions;
using Bit.Core.Models;
using IdentityServer3.Core.Models;
using System;
using System.DirectoryServices.AccountManagement;
using System.Threading;
using System.Threading.Tasks;

namespace Bit.IdentityServer.Implementations
{
    public class ActiveDirectoryUserServiceProvider : UserService
    {
        public virtual AppEnvironment AppEnvironment { get; set; } = default!;

        public override Task<BitJwtToken> LocalLogin(LocalAuthenticationContext context, CancellationToken cancellationToken)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            string userName = context.UserName;
            string password = context.Password;
            string activeDirectoryName = AppEnvironment.GetConfig<string>(AppEnvironment.KeyValues.IdentityServer.ActiveDirectoryName) ?? throw new InvalidOperationException($"{nameof(AppEnvironment.KeyValues.IdentityServer.ActiveDirectoryName)} is null");
            using (PrincipalContext principalContext = new PrincipalContext(ContextType.Domain, activeDirectoryName))
            {
                string userNameAsWinUserName = userName;

                if (!userNameAsWinUserName.Contains(activeDirectoryName, StringComparison.InvariantCultureIgnoreCase))
                    userNameAsWinUserName = $"{activeDirectoryName}\\{userNameAsWinUserName}";

                if (principalContext.ValidateCredentials(userNameAsWinUserName, password))
                {
                    using (UserPrincipal user = UserPrincipal.FindByIdentity(principalContext, IdentityType.SamAccountName, userNameAsWinUserName))
                    {
                        if (user != null)
                            return Task.FromResult(new BitJwtToken { UserId = user.Sid.Value });
                    }
                }
            }

            throw new DomainLogicException("UserInActiveDirectoryCouldNotBeFound");
        }

        public override Task<bool> UserIsActiveAsync(IsActiveContext context, BitJwtToken bitJwtToken, CancellationToken cancellationToken)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            string activeDirectoryName = AppEnvironment.GetConfig<string>(AppEnvironment.KeyValues.IdentityServer.ActiveDirectoryName) ?? throw new InvalidOperationException($"{nameof(AppEnvironment.KeyValues.IdentityServer.ActiveDirectoryName)} is null");

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
