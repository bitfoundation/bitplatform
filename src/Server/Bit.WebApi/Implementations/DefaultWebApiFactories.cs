using Bit.Core.Contracts;
using Bit.WebApi.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Web.Http.Description;
using Microsoft.Web.Http.Routing;
using System;
using System.Web.Http;
using System.Web.Http.Routing;

namespace Bit.WebApi.Implementations
{
    public static class DefaultWebApiFactories
    {
        public static IDependencyManager RegisterWebApiFactories(this IDependencyManager dependencyManager)
        {
            dependencyManager.RegisterUsing(resolver => new WebApiHttpServerFactory((webApiConfiguration) => WebApiHttpServerFactory<HttpServer>(resolver, webApiConfiguration)), lifeCycle: DependencyLifeCycle.Transient, overwriteExisting: false);
            dependencyManager.RegisterUsing(resolver => new WebApiInlineConstraintResolverFactory(() => WebApiInlineConstraintResolverFactory<DefaultInlineConstraintResolver>(resolver)), lifeCycle: DependencyLifeCycle.Transient, overwriteExisting: false);
            dependencyManager.RegisterUsing(resolver => new WebApiExplorerFactory((webApiConfiguration) => WebApiExplorerFactory(webApiConfiguration)), lifeCycle: DependencyLifeCycle.Transient, overwriteExisting: false);

            return dependencyManager;
        }

        public static THttpServer WebApiHttpServerFactory<THttpServer>(IDependencyResolver resolver, HttpConfiguration webApiConfiguration)
            where THttpServer : HttpServer
        {
            return ActivatorUtilities.CreateInstance<THttpServer>(resolver.Resolve<IServiceProvider>(), new object[] { webApiConfiguration });
        }

        public static TDefaultInlineConstraintResolver WebApiInlineConstraintResolverFactory<TDefaultInlineConstraintResolver>(IDependencyResolver resolver)
            where TDefaultInlineConstraintResolver : DefaultInlineConstraintResolver
        {
            var inlineConstraintResolver = ActivatorUtilities.CreateInstance<TDefaultInlineConstraintResolver>(resolver.Resolve<IServiceProvider>(), Array.Empty<object>());

            inlineConstraintResolver.ConstraintMap.Add("apiVersion", typeof(ApiVersionRouteConstraint));

            return inlineConstraintResolver;
        }

        public static VersionedApiExplorer WebApiExplorerFactory(HttpConfiguration webApiConfiguration)
        {
            VersionedApiExplorer apiExplorer = webApiConfiguration.AddVersionedApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'VVV";
                options.SubstituteApiVersionInUrl = true;
            });

            return apiExplorer;
        }
    }
}
