using System.Threading.Tasks;
using Bit.Core.Contracts;
using Bit.Core.Models;
using Bit.Owin.Contracts;
using Microsoft.Owin;

namespace Bit.Owin.Middlewares
{
    public class SignOutPageMiddleware : OwinMiddleware
    {
        public SignOutPageMiddleware(OwinMiddleware next)
            : base(next)
        {
        }

        public override Task Invoke(IOwinContext context)
        {
            IDependencyResolver dependencyResolver = context.GetDependencyResolver();

            AppEnvironment activeAppEnvironment = dependencyResolver.Resolve<AppEnvironment>();

            string defaultPath = activeAppEnvironment.GetHostVirtualPath();
            string defaultPathWithoutEndingSlashIfIsNotRoot = defaultPath == "/" ? defaultPath : defaultPath.Substring(0, defaultPath.Length - 1);

            IUrlStateProvider urlStateProvider = dependencyResolver.Resolve<IUrlStateProvider>();

            dynamic state = urlStateProvider.GetState(context.Request.Uri);

            bool autoCloseIsTrue = false;

            try
            {
                autoCloseIsTrue = state.AutoClose == true;
            }
            catch { }

            string singOutPage = $@"
<html>
    <head>
        <title>Signing out... Please wait</title>
        <script type='application/javascript'>
            localStorage.removeItem('{defaultPath}access_token');
            localStorage.removeItem('{defaultPath}expires_in');
            localStorage.removeItem('{defaultPath}id_token');
            localStorage.removeItem('{defaultPath}login_date');
            localStorage.removeItem('{defaultPath}scope');
            localStorage.removeItem('{defaultPath}session_state');
            localStorage.removeItem('{defaultPath}state');
            localStorage.removeItem('{defaultPath}token_type');
            var cookies = document.cookie.split('; ');
            for (var i = 0; i < cookies.length; i++)
            {{
                var cookie = cookies[i];
                var eqPos = cookie.indexOf('=');
                var name = eqPos > -1 ? cookie.substr(0, eqPos) : cookie;
                if(name == 'access_token' || name == 'token_type')
                    document.cookie = name + '=;expires=Thu, 01 Jan 1970 00:00:00 GMT;path={defaultPathWithoutEndingSlashIfIsNotRoot}';
            }}
            {(autoCloseIsTrue ? "window.close();" : $"location = '{defaultPath}';")}
        </script>
    </head>
    <body>
        <h1>Signing out... Please wait</h1>
    </body>
</html>
";

            context.Response.ContentType = "text/html; charset=utf-8";

            return context.Response.WriteAsync(singOutPage, context.Request.CallCancelled);
        }
    }
}