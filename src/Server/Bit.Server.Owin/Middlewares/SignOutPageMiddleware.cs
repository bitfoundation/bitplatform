﻿using System;
using System.Threading.Tasks;
using Bit.Core.Models;
using Bit.Owin.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.Owin.Middlewares
{
    public class SignOutPageMiddleware
    {
        private readonly RequestDelegate _next;

        public SignOutPageMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public Task Invoke(HttpContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            AppEnvironment activeAppEnvironment = context.RequestServices.GetService<AppEnvironment>();

            string defaultPath = activeAppEnvironment.GetHostVirtualPath();
            string defaultPathWithoutEndingSlashIfIsNotRoot = defaultPath == "/" ? defaultPath : defaultPath.Substring(0, defaultPath.Length - 1);

            IUrlStateProvider urlStateProvider = context.RequestServices.GetService<IUrlStateProvider>();

            dynamic state = urlStateProvider.GetState(new Uri(context.Request.GetEncodedUrl()));

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

            return context.Response.WriteAsync(singOutPage, context.RequestAborted);
        }
    }
}
