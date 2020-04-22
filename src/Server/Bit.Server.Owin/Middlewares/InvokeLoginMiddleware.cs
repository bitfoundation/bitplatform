using Bit.Core.Contracts;
using Bit.Core.Models;
using Microsoft.Owin;
using System;
using System.Threading.Tasks;

namespace Bit.Owin.Middlewares
{
    public class InvokeLoginMiddleware : OwinMiddleware
    {
        public InvokeLoginMiddleware(OwinMiddleware next)
            : base(next)
        {
        }

        private AppEnvironment? _App;

        public override Task Invoke(IOwinContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            IDependencyResolver dependencyResolver = context.GetDependencyResolver();

            if (_App == null)
            {
                _App = dependencyResolver.Resolve<AppEnvironment>();
            }

            IRandomStringProvider randomStringProvider = dependencyResolver.Resolve<IRandomStringProvider>();

            string client_Id = context.Request.Query["client_id"] ?? _App.GetSsoDefaultClientId();
            string afterLoginRedirect_uri = context.Request.Query["redirect_uri"] ?? $"{context.Request.Scheme}://{context.Request.Host.Value}{_App.GetHostVirtualPath()}SignIn";

            string ssoRedirectUri = $"{_App.GetSsoUrl()}/connect/authorize?scope={string.Join(" ", _App.Security.Scopes)}&client_id={client_Id}&redirect_uri={afterLoginRedirect_uri}&response_type=id_token token";

            string stateArgs = context.Request.Query["state"] ?? "{}";

            string nonce = randomStringProvider.GetRandomString(12);

            string url = $"{ssoRedirectUri}&state={stateArgs}&nonce={nonce}";

            if (context.Request.Query["acr_values"] != null)
                url += $"&acr_values={context.Request.Query["acr_values"]}";

            context.Response.Redirect(url);

            return Task.CompletedTask;
        }
    }
}
