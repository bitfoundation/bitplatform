using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Reflection;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class IServiceCollectionExtensions
    {
        public static IMvcCoreBuilder AddWebApiCore(this IServiceCollection services, params Assembly[] controllersAssemblies)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            if (controllersAssemblies == null)
                throw new ArgumentNullException(nameof(controllersAssemblies));

            controllersAssemblies = controllersAssemblies.Any() ? controllersAssemblies : new[] { Assembly.GetCallingAssembly() };

            IMvcCoreBuilder builder = services.AddMvcCore()
                .AddJsonFormatters();

            controllersAssemblies.ToList().ForEach(asm =>
            {
                builder.AddApplicationPart(asm);
            });

            builder.AddControllersAsServices();

            return builder;
        }
    }
}
