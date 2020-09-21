using Bit.Core.Contracts;
using Bit.Core.Models;
using Microsoft.Owin;
using System;
using System.Threading.Tasks;

namespace Bit.Owin.Middlewares
{
    public class SignInPageMiddleware : OwinMiddleware
    {
        public SignInPageMiddleware(OwinMiddleware next)
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

            string defaultPath = _App.GetHostVirtualPath();
            string defaultPathWithoutEndingSlashIfIsNotRoot = defaultPath == "/" ? defaultPath : defaultPath.Substring(0, defaultPath.Length - 1);

            string signInPage = $@"
<html>
    <head>
        <title>Signing in... Please wait</title>
        <script type='application/javascript'>
            var parts = location.hash.replace('#','').split('&');
            var expiresTimeInSeconds = Number(parts[3].split('=')[1]);
            var expiresDate = new Date();
            expiresDate.setTime(expiresDate.getTime() + (expiresTimeInSeconds * 1000));
            var expiresDateAsUTCString = expiresDate.toUTCString();
            for (var i = 0; i < parts.length; i++) {{
                var partStr = parts[i];
                var keyValue = partStr.split('=');
                var key = keyValue[0];
                var value = keyValue[1];
                if (key == 'access_token' || key == 'token_type'){{
                    document.cookie = partStr + ';expires=' + expiresDateAsUTCString + ';path={defaultPathWithoutEndingSlashIfIsNotRoot}';
                }}
                localStorage['{defaultPath}' + key] = value;
            }}
            localStorage['{defaultPath}login_date'] = new Date();
            var state = JSON.parse(decodeURIComponent(localStorage['{defaultPath}state'].replace(/\+/g, ' ')));
            localStorage['{defaultPath}state'] = JSON.stringify(state);
            if(state.AutoClose == null || state.AutoClose == false) {{
                location = state.pathname || '{defaultPath}';
            }}
            else {{
                window.close();
            }}
        </script>
    </head>
    <body>
        <h1>Signing in... Please wait</h1>
    </body>
</html>
";

            context.Response.ContentType = "text/html; charset=utf-8";

            return context.Response.WriteAsync(signInPage, context.Request.CallCancelled);
        }
    }
}