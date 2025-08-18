using Bit.Core.Contracts;
using Bit.Core.Models;
using Bit.IdentityServer.Contracts;
using Bit.Owin.Contracts;
using Microsoft.Owin;
using System.Threading;
using System.Threading.Tasks;

namespace Bit.IdentityServer.Implementations
{
    public abstract class UserService
    {
        public virtual IOwinContext OwinContext { get; set; } = default!;

        public virtual ILogger Logger { get; set; } = default!;

        public virtual IScopeStatusManager ScopeStatusManager { get; set; } = default!;

        public virtual IUserInformationProvider UserInformationProvider { get; set; } = default!;

        public virtual IExceptionToHttpErrorMapper ExceptionToHttpErrorMapper { get; set; } = default!;

        public abstract Task<BitJwtToken> LocalLogin(LocalAuthenticationContext context, CancellationToken cancellationToken);
    }
}
