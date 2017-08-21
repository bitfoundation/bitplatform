using System;
using System.Collections.Generic;
using System.Reflection;

namespace Bit.Core.Contracts
{
    public enum DependencyLifeCycle
    {
        SingleInstance,
        InstancePerLifetimeScope
    }

    public interface IDependencyResolver : IServiceProvider, IDisposable
    {
        TService Resolve<TService>(string name = null);

        TService ResolveOptional<TService>(string name = null)
            where TService : class;

        IEnumerable<TService> ResolveAll<TService>(string name = null);

        object Resolve(TypeInfo serviceType, string name = null);

        object ResolveOptional(TypeInfo serviceType, string name = null);

        IEnumerable<object> ResolveAll(TypeInfo serviceType, string name = null);

        bool IsRegistered<TService>();

        bool IsRegistered(TypeInfo serviceType);
    }

    /// <summary>
    /// Registers dependencies such as repositories and middlewares such as web api, signalr etc
    /// </summary>
    public interface IDependencyManager : IDependencyResolver
    {
        IDependencyManager Init();

        IDependencyManager BuildContainer();

        IDependencyManager RegisterAssemblyTypes(Assembly[] assemblies, Predicate<TypeInfo> predicate = null);

        bool IsInited();

        IDependencyManager Register<TService, TImplementation>(string name = null,
            DependencyLifeCycle lifeCycle = DependencyLifeCycle.InstancePerLifetimeScope, bool overwriteExciting = true)
            where TImplementation : class, TService;

        IDependencyManager Register(TypeInfo serviceType, TypeInfo implementationType, string name = null,
    DependencyLifeCycle lifeCycle = DependencyLifeCycle.InstancePerLifetimeScope, bool overwriteExciting = true);

        IDependencyManager RegisterInstance<TService>(TService implementationInstance, bool overwriteExciting = true, string name = null)
            where TService : class;

        IDependencyManager RegisterInstance(object obj, TypeInfo serviceType, bool overwriteExciting = true, string name = null);

        /// <summary>
        /// Register an un-parameterised generic type, e.g. IRepository&lt;&gt;. Concrete types will be made as they are requested, e.g. with IRepository&lt;Customer&gt;
        /// </summary>
        IDependencyManager RegisterGeneric(TypeInfo serviceType, TypeInfo implementationType, DependencyLifeCycle lifeCycle = DependencyLifeCycle.InstancePerLifetimeScope);

        IDependencyManager RegisterUsing<TService>(Func<TService> factory, string name = null,
            DependencyLifeCycle lifeCycle = DependencyLifeCycle.InstancePerLifetimeScope, bool overwriteExciting = true);

        IDependencyManager RegisterUsing(Func<object> factory, TypeInfo serviceType, string name = null, DependencyLifeCycle lifeCycle = DependencyLifeCycle.InstancePerLifetimeScope, bool overwriteExciting = true);

        IDependencyResolver CreateChildDependencyResolver(Action<IDependencyManager> childDependencyManager = null);
    }
}