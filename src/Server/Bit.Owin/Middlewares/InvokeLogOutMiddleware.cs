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

        public override async Task Invoke(IOwinContext context)
        {
            IDependencyResolver dependencyResolver = context.GetDependencyResolver();

            IAppEnvironmentProvider appEnvironmentProvider = dependencyResolver
                .Resolve<IAppEnvironmentProvider>();

            AppEnvironment activeAppEnvironment = appEnvironmentProvider.GetActiveAppEnvironment();

            string redirectUriHost = $"{context.Request.Scheme}://{context.Request.Host.Value}{activeAppEnvironment.GetHostVirtualPath()}SignOut";

            string redirectUri = $"{activeAppEnvironment.GetSsoUrl()}/connect/endsession?post_logout_redirect_uri={redirectUriHost}";

            context.Response.Redirect(redirectUri + "&id_token_hint=" + context.Request.Query["id_token"]);

            context.Authentication.SignOut("custom", "Barear");
        }
    }
}
