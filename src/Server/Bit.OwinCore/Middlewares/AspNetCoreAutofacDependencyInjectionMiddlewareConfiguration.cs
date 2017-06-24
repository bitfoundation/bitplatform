using Autofac;
using Autofac.Integration.Owin;
using Bit.Owin.Contracts;
using Bit.Owin.Middlewares;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Owin;
using Owin;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace Bit.OwinCore.Middlewares
{
    public class AspNetCoreAutofacDependencyInjectionMiddlewareConfiguration : IOwinMiddlewareConfiguration
    {
        public virtual void Configure(IAppBuilder owinApp)
        {
            owinApp.Use<AspNetCoreAutofacDependencyInjectionMiddleware>();

            owinApp.Use<AutofacScopeBasedDependencyResolverMiddleware>();
        }
    }

    public class AspNetCoreAutofacDependencyInjectionMiddleware : OwinMiddleware
    {
        public AspNetCoreAutofacDependencyInjectionMiddleware(OwinMiddleware next)
            : base(next)
        {

        }

        static AspNetCoreAutofacDependencyInjectionMiddleware()
        {
            TypeInfo autofacConstantsType = typeof(OwinContextExtensions).GetTypeInfo().Assembly.GetType("Autofac.Integration.Owin.Constants").GetTypeInfo();

            FieldInfo owinLifetimeScopeKeyField = autofacConstantsType.GetField("OwinLifetimeScopeKey", BindingFlags.Static | BindingFlags.NonPublic);

            if (owinLifetimeScopeKeyField == null)
                throw new InvalidOperationException($"OwinLifetimeScopeKey field could not be found in {nameof(OwinContextExtensions)} ");

            OwinLifetimeScopeKey = (string)owinLifetimeScopeKeyField.GetValue(null);
        }

        private static readonly string OwinLifetimeScopeKey;

        public override async Task Invoke(IOwinContext context)
        {
            HttpContext aspNetCoreContext = (HttpContext)context.Environment["Microsoft.AspNetCore.Http.HttpContext"];

            aspNetCoreContext.Items["OwinContext"] = context;

            ILifetimeScope autofacScope = aspNetCoreContext.RequestServices.GetService<ILifetimeScope>();

            context.Set(OwinLifetimeScopeKey, autofacScope);

            await Next.Invoke(context);
        }
    }
}
