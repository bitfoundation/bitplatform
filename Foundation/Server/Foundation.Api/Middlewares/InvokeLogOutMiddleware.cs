using Foundation.Core.Contracts;
using Foundation.Core.Models;
using Microsoft.Owin;
using System.Threading.Tasks;

namespace Foundation.Api.Middlewares
{
    public class InvokeLogOutMiddleware : OwinMiddleware
    {
        public InvokeLogOutMiddleware(OwinMiddleware next)
            : base(next)
        {
        }

        private string _baseRedirectUri = null;

        public override async Task Invoke(IOwinContext context)
        {
            IDependencyResolver dependencyResolver = context.GetDependencyResolver();

            if (_baseRedirectUri == null)
            {
                IAppEnvironmentProvider appEnvironmentProvider = dependencyResolver
                    .Resolve<IAppEnvironmentProvider>();

                AppEnvironment activEnvironment = appEnvironmentProvider.GetActiveAppEnvironment();

                _baseRedirectUri = $"{activEnvironment.Security.SSOServerUrl}connect/endsession?post_logout_redirect_uri={activEnvironment.GetConfig("ClientHostBaseUri", context.Request.Host.Value)}{activEnvironment.GetConfig("ClientHostVirtualPath", "/")}SignOut";
            }

            context.Response.Redirect(_baseRedirectUri + "&id_token_hint=" + context.Request.Query["id_token"]);

            context.Authentication.SignOut("custom", "Barear");
        }
    }
}
