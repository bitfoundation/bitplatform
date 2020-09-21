using Bit.Core.Contracts;
using Bit.Core.Models;
using Microsoft.Owin;
using System;
using System.Threading.Tasks;

namespace Bit.Owin.Middlewares
{
    public class RedirectToSsoIfNotLoggedInMiddleware : OwinMiddleware
    {
        public RedirectToSsoIfNotLoggedInMiddleware(OwinMiddleware next)
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

            string redirectUriHost = $"{context.Request.Scheme}://{context.Request.Host.Value}{_App.GetHostVirtualPath()}SignIn";
            string redirectUri = $"{_App.GetSsoUrl()}/connect/authorize?scope={string.Join(" ", _App.Security.Scopes)}&client_id={_App.GetSsoDefaultClientId()}&redirect_uri={redirectUriHost}&response_type=id_token token";

            string pathname = _App.GetHostVirtualPath() + ((context.Request.Path != null && !string.IsNullOrEmpty(context.Request.Path.Value) && context.Request.Path.Value.Length > 0) ? context.Request.Path.Value.Substring(1) : string.Empty);

            string state = $@"{{""pathname"":""{pathname}""}}";

            string nonce = randomStringProvider.GetRandomString(12);

            context.Response.Redirect($"{redirectUri}&state={state}&nonce={nonce}");

            return Task.CompletedTask;
        }
    }
}