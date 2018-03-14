using Bit.Core.Contracts;
using Bit.Core.Models;
using Microsoft.Owin;
using System.Threading.Tasks;

namespace Bit.Owin.Middlewares
{
    public class InvokeLogOutMiddleware : OwinMiddleware
    {
        public InvokeLogOutMiddleware(OwinMiddleware next)
            : base(next)
        {
        }

        private AppEnvironment _App;

        public override Task Invoke(IOwinContext context)
        {
            IDependencyResolver dependencyResolver = context.GetDependencyResolver();

            if (_App == null)
            {
                _App = dependencyResolver.Resolve<AppEnvironment>();
            }

            string afterLogoutRedirect_uri = context.Request.Query["redirect_uri"] ?? $"{context.Request.Scheme}://{context.Request.Host.Value}{_App.GetHostVirtualPath()}SignOut";

            string ssoRedirectUri = $"{_App.GetSsoUrl()}/connect/endsession?post_logout_redirect_uri={afterLogoutRedirect_uri}";

            string stateArgs = context.Request.Query["state"] ?? "{}";

            context.Response.Redirect($"{ssoRedirectUri}&id_token_hint={(context.Request.Query["id_token"])}&state={stateArgs}");

            context.Authentication.SignOut("custom", "Bearer");

            return Task.CompletedTask;
        }
    }
}
