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

            string redirect_uri_host = $"{context.Request.Scheme}://{context.Request.Host.Value}{activeAppEnvironment.GetHostVirtualPath()}SignIn";
            string redirect_uri = $"{activeAppEnvironment.GetSsoUrl()}/connect/authorize?scope={string.Join(" ", activeAppEnvironment.Security.Scopes)}&client_id={activeAppEnvironment.Security.ClientName}&redirect_uri={redirect_uri_host}&response_type=id_token token";

            string pathname = context.Request.Path.Value;

            string stateArgs = string.Join(string.Empty, context.Request.Path.Value.SkipWhile(c => c == '/'));

            string nonce = randomStringProvider.GetRandomNonSecureString(12);

            context.Response.Redirect($"{redirect_uri}&state={stateArgs}&nonce={nonce}");
        }
    }
}
