using System;
using System.Threading.Tasks;
using Autofac;
using Autofac.Integration.Owin;
using Bit.Core.Contracts;
using Bit.Owin.Contracts;
using Bit.Owin.Implementations;
using Microsoft.Owin;
using Owin;

namespace Bit.Owin.Middlewares
{
    /// <summary>
    /// This will create autofac scope for every request in owin pipeline.
    /// You can use created scope via constructor injection in web api and odata controllers
    /// You can use created scope using owin context's Get method.
    /// </summary>
    public class AutofacDependencyInjectionMiddlewareConfiguration : IOwinMiddlewareConfiguration
    {
        private ILifetimeScope _lifetimeScope = default!;

        public virtual IAutofacDependencyManager DependencyManager
        {
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(DependencyManager));

                _lifetimeScope = value.GetContainer();
            }
        }

        public virtual void Configure(IAppBuilder owinApp)
        {
            if (owinApp == null)
                throw new ArgumentNullException(nameof(owinApp));

            owinApp.UseAutofacLifetimeScopeInjector(_lifetimeScope);

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

        public override Task Invoke(IOwinContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            IAutofacDependencyManager childDependencyManager = new AutofacDependencyManager();

            childDependencyManager.UseContainer(context.GetAutofacLifetimeScope());

            context.Set("DependencyResolver", (IDependencyResolver)childDependencyManager);

            return Next.Invoke(context);
        }
    }
}