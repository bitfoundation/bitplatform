#if DotNet
using Bit.Core;
using Bit.Core.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Reflection;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class IServiceCollectionExtensions
    {
        public static IMvcCoreBuilder AddWebApiCore(this IDependencyManager dependencyManager, params Assembly[] controllersAssemblies)
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            if (controllersAssemblies == null)
                throw new ArgumentNullException(nameof(controllersAssemblies));

            controllersAssemblies = AssemblyContainer.Current.AssembliesWithDefaultAssemblies(controllersAssemblies);

            IServiceCollection services = ((IServiceCollectionAccessor)dependencyManager).ServiceCollection;

            IMvcCoreBuilder builder = services.AddMvcCore()
                .AddJsonFormatters()
                .AddAuthorization() // ToDo: Needs enhancements!
                .AddFormatterMappings()
                //.SetCompatibilityVersion(CompatibilityVersion.Version_2_1) // ToDo
                .AddDataAnnotations();

            foreach (Assembly asm in controllersAssemblies)
            {
                builder.AddApplicationPart(asm);
            }

            dependencyManager.RegisterAssemblyTypes(controllersAssemblies, t => (t.GetCustomAttribute<ControllerAttribute>(inherit: true /*ApiControllerAttribute*/) != null || t.GetCustomAttribute<RouteAttribute>() != null) && t.GetCustomAttribute<NonControllerAttribute>() == null, lifeCycle: DependencyLifeCycle.Transient);

            builder.Services.Replace(ServiceDescriptor.Transient<IControllerActivator, ServiceBasedControllerActivator>());

            return builder;
        }
    }
}
#endif