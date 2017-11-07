using System.Linq;
using System.Threading.Tasks;
using Bit.Core.Contracts;
using Bit.Core.Models;
using Microsoft.Owin;

namespace Bit.Owin.Middlewares
{
    public class InvokeLoginMiddleware : OwinMiddleware
    {
        public InvokeLoginMiddleware(OwinMiddleware next)
            : base(next)
        {
        }

        public override async Task Invoke(IOwinContext context)
        {
            IDependencyResolver dependencyResolver = context.GetDependencyResolver();

            IRandomStringProvider randomStringProvider = dependencyResolver.Resolve<IRandomStringProvider>();

            IAppEnvironmentProvider appEnvironmentProvider = dependencyResolver.Resolve<IAppEnvironmentProvider>();

            AppEnvironment activeAppEnvironment = appEnvironmentProvider.GetActiveAppEnvironment();

            string redirectUriHost = $"{context.Request.Scheme}://{context.Request.Host.Value}{activeAppEnvironment.GetHostVirtualPath()}SignIn";
            string redirectUri = $"{activeAppEnvironment.GetSsoUrl()}/connect/authorize?scope={string.Join(" ", activeAppEnvironment.Security.Scopes)}&client_id={activeAppEnvironment.Security.ClientId}&redirect_uri={redirectUriHost}&response_type=id_token token";

            string stateArgs = string.Join(string.Empty, context.Request.Path.Value.SkipWhile(c => c == '/'));

            string nonce = randomStringProvider.GetRandomNonSecureString(12);

            context.Response.Redirect($"{redirectUri}&state={stateArgs}&nonce={nonce}");
        }
    }
}
