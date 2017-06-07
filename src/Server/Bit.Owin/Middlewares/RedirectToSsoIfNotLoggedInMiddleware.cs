using System.Threading.Tasks;
using Bit.Core.Contracts;
using Bit.Core.Models;
using Microsoft.Owin;

namespace Bit.Owin.Middlewares
{
    public class RedirectToSsoIfNotLoggedInMiddleware : OwinMiddleware
    {
        public RedirectToSsoIfNotLoggedInMiddleware(OwinMiddleware next)
            : base(next)
        {
        }

        private string _baseRedirectUri = null;

        public override Task Invoke(IOwinContext context)
        {
            IDependencyResolver dependencyResolver = context.GetDependencyResolver();

            IRandomStringProvider randomStringProvider = dependencyResolver.Resolve<IRandomStringProvider>();

            IAppEnvironmentProvider appEnvironmentProvider = dependencyResolver.Resolve<IAppEnvironmentProvider>();

            AppEnvironment activEnvironment = appEnvironmentProvider.GetActiveAppEnvironment();

            if (_baseRedirectUri == null)
            {
                _baseRedirectUri = $"{activEnvironment.Security.SSOServerUrl}/connect/authorize?scope={string.Join(" ", activEnvironment.Security.Scopes)}&client_id={activEnvironment.Security.ClientName}&redirect_uri={activEnvironment.GetConfig("ClientHostBaseUri", context.Request.Host.Value)}{activEnvironment.GetConfig("ClientHostVirtualPath", "/")}SignIn&response_type=id_token token";
            }

            string pathname = activEnvironment.GetConfig("ClientHostVirtualPath", "/") + (context.Request.Path.HasValue ? context.Request.Path.Value.Substring(1) : string.Empty);

            string state = $@"{{""pathname"":""{pathname}""}}";

            string nonce = randomStringProvider.GetRandomNonSecureString(12);

            string redirectUrl = $"{_baseRedirectUri}&state={state}&nonce={nonce}";

            context.Response.Redirect(redirectUrl);

            return Task.CompletedTask;
        }
    }
}