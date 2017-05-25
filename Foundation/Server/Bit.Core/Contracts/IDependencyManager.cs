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
        TContract Resolve<TContract>(string name = null);

        TContract ResolveOptional<TContract>(string name = null)
            where TContract : class;

        IEnumerable<TContract> ResolveAll<TContract>(string name = null);

        object Resolve(TypeInfo contractType, string name = null);

        object ResolveOptional(TypeInfo contractType, string name = null);

        IEnumerable<object> ResolveAll(TypeInfo contractType, string name = null);

        bool IsRegistered<TContract>();

        bool IsRegistered(TypeInfo contractType);
    }

    public interface IDependencyManager : IDependencyResolver
    {
        IDependencyManager Init();

        IDependencyManager BuildContainer();

        IDependencyManager RegisterHubs(params Assembly[] assemblies);

        IDependencyManager RegisterApiControllers(params Assembly[] assemblies);

        bool IsInited();

        IDependencyManager Register<TContract, TService>(string name = null,
            DependencyLifeCycle lifeCycle = DependencyLifeCycle.InstancePerLifetimeScope, bool overwriteExciting = true)
            where TService : class, TContract;

        IDependencyManager Register(TypeInfo contractType, TypeInfo serviceType, string name = null,
    DependencyLifeCycle lifeCycle = DependencyLifeCycle.InstancePerLifetimeScope, bool overwriteExciting = true);

        IDependencyManager RegisterInstance<TContract>(TContract implementationInstance, bool overwriteExciting = true, string name = null)
            where TContract : class;

        IDependencyManager RegisterInstance(object obj, TypeInfo contractType, bool overwriteExciting = true, string name = null);

        IDependencyManager RegisterGeneric(TypeInfo contractType, TypeInfo serviceType, DependencyLifeCycle lifeCycle);

        IDependencyManager RegisterUsing<TContract>(Func<TContract> factory, string name = null,
            DependencyLifeCycle lifeCycle = DependencyLifeCycle.InstancePerLifetimeScope, bool overwriteExciting = true);

        IDependencyResolver CreateChildDependencyResolver(Action<IDependencyManager> childDependencyManager = null);
    }
}