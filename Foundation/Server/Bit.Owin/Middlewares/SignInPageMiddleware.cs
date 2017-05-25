using System.Threading.Tasks;
using Bit.Core.Contracts;
using Bit.Core.Models;
using Microsoft.Owin;

namespace Bit.Owin.Middlewares
{
    public class SignInPageMiddleware : OwinMiddleware
    {
        public SignInPageMiddleware(OwinMiddleware next)
            : base(next)
        {
        }

        public override async Task Invoke(IOwinContext context)
        {
            IDependencyResolver dependencyResolver = context.GetDependencyResolver();

            IAppEnvironmentProvider appEnvironmentProvider = dependencyResolver.Resolve<IAppEnvironmentProvider>();

            AppEnvironment activEnvironment = appEnvironmentProvider.GetActiveAppEnvironment();

            string defaultPath = activEnvironment.GetConfig("ClientHostVirtualPath", "/");

            string signInPage = $@"
<html>
    <head>
        <title>Signing in... Please wait</title>
        <script type='application/javascript'>
            var parts = location.hash.replace('#','').split('&');
            var expireTimeInSeconds = Number(parts[3].split('=')[1]);
            var now = new Date();
            var time = now.getTime();
            var expireTime = time + (expireTimeInSeconds * 1000);
            now.setTime(expireTime);
            var nowAsGMTString = now.toGMTString();
            for (var i = 0; i < parts.length; i++) {{
                var partStr = parts[i];
                var keyValue = partStr.split('=');
                var key = keyValue[0];
                var value = keyValue[1];
                if (key == 'access_token' || key == 'token_type'){{
                    document.cookie = partStr + ';expires=' + nowAsGMTString + ';path={defaultPath}';
                }}
                localStorage[key] = value;
            }}
            localStorage['login_date'] = new Date();
            var state = JSON.parse(decodeURIComponent(localStorage.state.replace(/\+/g, ' ')));
            localStorage['state'] = JSON.stringify(state);
            location = state.pathname || '{defaultPath}';
        </script>
    </head>
    <body>
        <h1>Signing in... Please wait</h1>
    </body>
</html>
";

            context.Response.ContentType = "text/html; charset=utf-8";

            await context.Response.WriteAsync(signInPage, context.Request.CallCancelled);
        }
    }
}