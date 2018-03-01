using System.Linq;
using System.Threading.Tasks;
using Bit.Core.Contracts;
using Bit.Core.Models;
using Microsoft.Owin;
using System.Net.Http;

namespace Bit.Owin.Middlewares
{
    public class InvokeLoginMiddleware : OwinMiddleware
    {
        public InvokeLoginMiddleware(OwinMiddleware next)
            : base(next)
        {
        }

        private AppEnvironment _App;

        public override async Task Invoke(IOwinContext context)
        {
            IDependencyResolver dependencyResolver = context.GetDependencyResolver();

            if (_App == null)
            {
                IAppEnvironmentProvider appEnvironmentProvider = dependencyResolver.Resolve<IAppEnvironmentProvider>();

                _App = appEnvironmentProvider.GetActiveAppEnvironment();
            }

            IRandomStringProvider randomStringProvider = dependencyResolver.Resolve<IRandomStringProvider>();

            string client_Id = context.Request.Uri.ParseQueryString()["client_id"] ?? _App.GetDefaultClientId();

            string redirectUriHost = $"{context.Request.Scheme}://{context.Request.Host.Value}{_App.GetHostVirtualPath()}SignIn";
            string redirectUri = $"{_App.GetSsoUrl()}/connect/authorize?scope={string.Join(" ", _App.Security.Scopes)}&client_id={client_Id}&redirect_uri={redirectUriHost}&response_type=id_token token";

            string stateArgs = string.Join(string.Empty, context.Request.Path.Value.SkipWhile(c => c == '/'));

            string nonce = randomStringProvider.GetRandomNonSecureString(12);

            context.Response.Redirect($"{redirectUri}&state={stateArgs}&nonce={nonce}");
        }
    }
}
