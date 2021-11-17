using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Bit.Core.Contracts
{
    public enum DependencyLifeCycle
    {
        SingleInstance,
        PerScopeInstance,
        Transient
    }

    public interface IDependencyResolver : IServiceProvider, IDisposable, IAsyncDisposable
    {
        TService Resolve<TService>(string? name = null)
            where TService : notnull;

        TService? ResolveOptional<TService>(string? name = null)
            where TService : class;

        IEnumerable<TService> ResolveAll<TService>(string? name = null)
            where TService : notnull;

        object Resolve(TypeInfo serviceType, string? name = null);

        object? ResolveOptional(TypeInfo serviceType, string? name = null);

        IEnumerable<object> ResolveAll(TypeInfo serviceType, string? name = null);

        bool IsRegistered<TService>()
            where TService : notnull;

        bool IsRegistered(TypeInfo serviceType);

        IPipelineAwareDisposable[] GetPipelineAwareDisposables();
    }

    public interface IServiceCollectionAccessor
    {
        IServiceCollection ServiceCollection { get; set; }
    }

    /// <summary>
    /// Registers dependencies such as repositories and middlewares such as web api, signalr etc
    /// </summary>
    public interface IDependencyManager : IDependencyResolver, IServiceProviderFactory<IDependencyManager>
    {
        /// <summary>
        /// By calling <see cref="Init"/> you can start registering your services.
        /// </summary>
        IDependencyManager Init();

        /// <summary>
        /// By calling <see cref="BuildContainer"/> you can start resolving your services.
        /// </summary>
        IDependencyManager BuildContainer();

        IDependencyManager RegisterAssemblyTypes(Assembly[] assemblies, Predicate<TypeInfo>? predicate = null, DependencyLifeCycle lifeCycle = DependencyLifeCycle.PerScopeInstance);

        bool ContainerIsBuilt();

        IDependencyManager Register<T>(string? name = null,
            DependencyLifeCycle lifeCycle = DependencyLifeCycle.PerScopeInstance, bool overwriteExisting = true)
            where T : class;

        IDependencyManager Register<TService, TImplementation>(string? name = null,
            DependencyLifeCycle lifeCycle = DependencyLifeCycle.PerScopeInstance, bool overwriteExisting = true)
            where TImplementation : class, TService;

        IDependencyManager Register(TypeInfo[] servicesType, TypeInfo implementationType, string? name = null,
            DependencyLifeCycle lifeCycle = DependencyLifeCycle.PerScopeInstance, bool overwriteExisting = true);

        IDependencyManager Register(TypeInfo serviceType, TypeInfo implementationType, string? name = null,
            DependencyLifeCycle lifeCycle = DependencyLifeCycle.PerScopeInstance, bool overwriteExisting = true);

        IDependencyManager RegisterInstance<TService>(TService implementationInstance, bool overwriteExisting = true, string? name = null)
            where TService : class;

        IDependencyManager RegisterInstance(object obj, TypeInfo[] servicesType, bool overwriteExisting = true, string? name = null);

        IDependencyManager RegisterInstance(object obj, TypeInfo serviceType, bool overwriteExisting = true, string? name = null);

        /// <summary>
        /// Register an unparameterized generic type, e.g. IRepository&lt;&gt;. Concrete types will be made as they are requested, e.g. with IRepository&lt;Customer&gt;
        /// </summary>
        IDependencyManager RegisterGeneric(TypeInfo[] servicesType, TypeInfo implementationType, DependencyLifeCycle lifeCycle = DependencyLifeCycle.PerScopeInstance);

        IDependencyManager RegisterGeneric(TypeInfo serviceType, TypeInfo implementationType, DependencyLifeCycle lifeCycle = DependencyLifeCycle.PerScopeInstance);

        IDependencyManager RegisterUsing<TService>(Func<IDependencyResolver, TService> factory, string? name = null,
            DependencyLifeCycle lifeCycle = DependencyLifeCycle.PerScopeInstance, bool overwriteExisting = true);

        IDependencyManager RegisterUsing(Func<IDependencyResolver, object> factory, TypeInfo[] servicesType, string? name = null, DependencyLifeCycle lifeCycle = DependencyLifeCycle.PerScopeInstance, bool overwriteExisting = true);

        IDependencyManager RegisterUsing(Func<IDependencyResolver, object> factory, TypeInfo serviceType, string? name = null, DependencyLifeCycle lifeCycle = DependencyLifeCycle.PerScopeInstance, bool overwriteExisting = true);

        IDependencyResolver CreateChildDependencyResolver(Action<IDependencyManager>? childDependencyManagerCustomizer = null);

        IDependencyManager Populate(IServiceCollection services);
    }
}
