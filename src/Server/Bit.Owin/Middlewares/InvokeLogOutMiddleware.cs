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

            string redirect_uri_host = $"{context.Request.Scheme}://{context.Request.Host.Value}{activeAppEnvironment.GetHostVirtualPath()}SignOut";

            string redirect_uri = $"{activeAppEnvironment.GetSsoUrl()}/connect/endsession?post_logout_redirect_uri={redirect_uri_host}";

            context.Response.Redirect(redirect_uri + "&id_token_hint=" + context.Request.Query["id_token"]);

            context.Authentication.SignOut("custom", "Barear");
        }
    }
}
