using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace Bit.Owin.Implementations
{
    public class BitAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public BitAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock)
            : base(options, logger, encoder, clock)
        {
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (Request.HttpContext.User?.Identity?.IsAuthenticated == true)
                return AuthenticateResult.Success(new AuthenticationTicket(Request.HttpContext.User, "JWT"));

            return AuthenticateResult.Fail("Authentication failed");
        }
    }
}
