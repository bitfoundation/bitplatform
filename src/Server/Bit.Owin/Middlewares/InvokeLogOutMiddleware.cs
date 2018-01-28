using System.Threading.Tasks;
using Bit.Core.Contracts;
using Bit.Core.Models;
using Microsoft.Owin;

namespace Bit.Owin.Middlewares
{
    public class InvokeLogOutMiddleware : OwinMiddleware
    {
        public InvokeLogOutMiddleware(OwinMiddleware next)
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

            string redirectUriHost = $"{context.Request.Scheme}://{context.Request.Host.Value}{_App.GetHostVirtualPath()}SignOut";

            string redirectUri = $"{_App.GetSsoUrl()}/connect/endsession?post_logout_redirect_uri={redirectUriHost}";

            context.Response.Redirect(redirectUri + "&id_token_hint=" + context.Request.Query["id_token"]);

            context.Authentication.SignOut("custom", "Barear");
        }
    }
}
