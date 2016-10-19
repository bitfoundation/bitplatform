using System;
using Autofac;
using Foundation.Api.Contracts;
using Foundation.Core.Contracts;
using Owin;
using Microsoft.Owin;
using System.Threading.Tasks;
using Autofac.Integration.Owin;
using Foundation.Api.Implementations;

namespace Foundation.Api.Middlewares
{
    /// <summary>
    /// This will create autofac scope for every request in owin pipeline.
    /// You can use created scope via constructor injection in web api and odata controllers
    /// You can use created scope using owin context's Get method.
    /// </summary>
    public class AutofacDependencyInjectionMiddlewareConfiguration : IOwinMiddlewareConfiguration
    {
        private readonly ILifetimeScope _container;

        protected AutofacDependencyInjectionMiddlewareConfiguration()
        {
        }

        public AutofacDependencyInjectionMiddlewareConfiguration(ILifetimeScope container)
        {
            if (container == null)
                throw new ArgumentNullException();

            _container = container;
        }

        public virtual void Configure(IAppBuilder owinApp)
        {
            if (owinApp == null)
                throw new ArgumentNullException(nameof(owinApp));

            owinApp.UseAutofacLifetimeScopeInjector(_container);

            owinApp.Use<AutofacScopeBasedDependencyResolverMiddleware>();
        }
    }

    /// <summary>
    /// By adding this middleware, you can call context.GetDependencyResolver() to get access to scope based dependency resolver of your owin context.
    /// </summary>
    public class AutofacScopeBasedDependencyResolverMiddleware : OwinMiddleware
    {
        public AutofacScopeBasedDependencyResolverMiddleware(OwinMiddleware next)
            : base(next)
        {
        }

        public override async Task Invoke(IOwinContext context)
        {
            context.Set<IDependencyResolver>("DependencyResolver", new AutofacScopeBasedDependencyResolver(context.GetAutofacLifetimeScope()));

            await Next.Invoke(context);
        }
    }
}