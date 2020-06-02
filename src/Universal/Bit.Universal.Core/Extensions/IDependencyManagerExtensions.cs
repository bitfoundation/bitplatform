using Autofac;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Reflection;
using Xamarin.Essentials.Interfaces;

namespace Bit.Core.Contracts
{
    public static class IDependencyManagerExtensions
    {
        public static IServiceCollection GetServiceCollection(this IDependencyManager dependencyManager)
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            return ((IServiceCollectionAccessor)dependencyManager).ServiceCollection;
        }

        public static ContainerBuilder GetContainerBuilder(this IDependencyManager dependencyManager)
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            return ((IAutofacDependencyManager)dependencyManager).GetContainerBuidler();
        }

        public static IDependencyManager RegisterXamarinEssentialsImplementations(this IDependencyManager dependencyManager)
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            foreach (Type xamEssentialsInterface in Assembly.Load("Essential.Interfaces").GetTypes().Where(t => t.IsPublic && t.IsInterface))
            {
                if (xamEssentialsInterface == typeof(IEssentialsImplementation))
                    continue;

                string wasmImplTypeName = $"Bit.Client.Web.Wasm.Implementation.{xamEssentialsInterface.Name.Replace("I", "WebAssembly")}, Bit.Client.Web.Wasm";

                Type? wasmImplType = Type.GetType(wasmImplTypeName, throwOnError: false);
                Type defaultImpl = Type.GetType($"Xamarin.Essentials.Implementation.{xamEssentialsInterface.Name.Substring(1)}Implementation, Essential.Interfaces", throwOnError: true)!;

                dependencyManager.Register(xamEssentialsInterface.GetTypeInfo(), (wasmImplType ?? defaultImpl).GetTypeInfo(), lifeCycle: wasmImplType != null ? DependencyLifeCycle.PerScopeInstance /*To make IJSRuntime accessible!*/ : DependencyLifeCycle.SingleInstance);
            }

            return dependencyManager;
        }
    }
}
