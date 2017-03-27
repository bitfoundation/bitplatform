using Foundation.Core.Contracts;
using Foundation.Core.Models;
using Microsoft.Owin;
using System.Linq;
using System.Threading.Tasks;

namespace Foundation.Api.Middlewares
{
    public class InvokeLoginMiddleware : OwinMiddleware
    {
        public InvokeLoginMiddleware(OwinMiddleware next)
            : base(next)
        {
        }

        private string _baseRedirectUri = null;

        public override async Task Invoke(IOwinContext context)
        {
            IDependencyResolver dependencyResolver = context.GetDependencyResolver();

            IRandomStringProvider randomStringProvider = dependencyResolver.Resolve<IRandomStringProvider>();

            if (_baseRedirectUri == null)
            {
                IAppEnvironmentProvider appEnvironmentProvider = dependencyResolver
                    .Resolve<IAppEnvironmentProvider>();

                AppEnvironment activEnvironment = appEnvironmentProvider.GetActiveAppEnvironment();

                _baseRedirectUri = $"{activEnvironment.Security.SSOServerUrl}/connect/authorize?scope={string.Join(" ", activEnvironment.Security.Scopes)}&client_id={activEnvironment.Security.ClientName}&redirect_uri={activEnvironment.GetConfig("ClientHostBaseUri", context.Request.Host.Value)}{activEnvironment.GetConfig("ClientHostVirtualPath", "/")}SignIn&response_type=id_token token";
            }

            string nonce = randomStringProvider.GetRandomNonSecureString(12);

            string stateArgs = string.Join(string.Empty, context.Request.Path.Value.SkipWhile(c => c == '/'));

            string redirectUrl = $"{_baseRedirectUri}&state={stateArgs}&nonce={nonce}";

            context.Response.Redirect(redirectUrl);
        }
    }
}
