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

        public override Task Invoke(IOwinContext context)
        {
            IDependencyResolver dependencyResolver = context.GetDependencyResolver();

            IRandomStringProvider randomStringProvider = dependencyResolver.Resolve<IRandomStringProvider>();

            IAppEnvironmentProvider appEnvironmentProvider = dependencyResolver.Resolve<IAppEnvironmentProvider>();

            AppEnvironment activeAppEnvironment = appEnvironmentProvider.GetActiveAppEnvironment();

            string redirectUriHost = $"{context.Request.Scheme}://{context.Request.Host.Value}{activeAppEnvironment.GetHostVirtualPath()}SignIn";
            string redirectUri = $"{activeAppEnvironment.GetSsoUrl()}/connect/authorize?scope={string.Join(" ", activeAppEnvironment.Security.Scopes)}&client_id={activeAppEnvironment.Security.ClientId}&redirect_uri={redirectUriHost}&response_type=id_token token";

            string pathname = activeAppEnvironment.GetHostVirtualPath() + (context.Request.Path.HasValue ? context.Request.Path.Value.Substring(1) : string.Empty);

            string state = $@"{{""pathname"":""{pathname}""}}";

            string nonce = randomStringProvider.GetRandomNonSecureString(12);

            context.Response.Redirect($"{redirectUri}&state={state}&nonce={nonce}");

            return Task.CompletedTask;
        }
    }
}