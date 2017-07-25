using System;
using System.Collections.Generic;
using System.Reflection;
using Autofac;
using Autofac.Builder;
using Bit.Core.Contracts;
using Bit.Owin.Contracts;

namespace Bit.Owin.Implementations
{
    public class AutofacDependencyManager : IDependencyManager, IAutofacDependencyManager
    {
        private ContainerBuilder _containerBuilder;
        private ILifetimeScope _container;

        public virtual IDependencyManager Init()
        {
            UseContainerBuilder(new ContainerBuilder());
            return this;
        }

        public void UseContainerBuilder(ContainerBuilder builder)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));
            if (_containerBuilder != null)
                throw new InvalidOperationException("Container builder has been set already");
            _containerBuilder = builder;
            _containerBuilder.Register((context, parameter) => (IDependencyManager)this).SingleInstance();
            _containerBuilder.Register((context, parameter) => (IServiceProvider)this).SingleInstance();
            _containerBuilder.Register((context, parameter) => (IAutofacDependencyManager)this).SingleInstance();
        }

        private IDependencyManager SetContainer(ILifetimeScope container)
        {
            if (container == null)
                throw new ArgumentNullException(nameof(container));
            if (_container != null)
                throw new InvalidOperationException("Container has been set already");
            _container = container;
            return this;
        }

        public virtual IDependencyManager BuildContainer()
        {
            SetContainer(_containerBuilder.Build());
            return this;
        }

        public virtual bool IsInited()
        {
            return _container != null;
        }

        public void UseContainer(ILifetimeScope lifetimeScope)
        {
            SetContainer(lifetimeScope);
        }

        public virtual ILifetimeScope GetContainer()
        {
            if (!IsInited())
                throw new InvalidOperationException("Container is not prepared, build it first.");

            return _container;
        }

        public virtual ContainerBuilder GetContainerBuidler()
        {
            if (_containerBuilder == null)
                throw new InvalidOperationException("Container builder is not prepared, Either call Init or UseContainerBuilder first");

            return _containerBuilder;
        }

        public virtual IDependencyResolver CreateChildDependencyResolver(Action<IDependencyManager> childDependencyManagerCustomizer = null)
        {
            IAutofacDependencyManager childDependencyManager = new AutofacDependencyManager();

            ILifetimeScope container = GetContainer().BeginLifetimeScope(containerBuilder =>
            {
                if (childDependencyManagerCustomizer != null)
                {
                    childDependencyManager.UseContainerBuilder(containerBuilder);
                    childDependencyManagerCustomizer((IDependencyManager)childDependencyManager);
                }
            });

            childDependencyManager.UseContainer(container);

            return (IDependencyResolver)childDependencyManager;
        }

        public virtual TContract Resolve<TContract>(string name = null)
        {
            ILifetimeScope container = GetContainer();

            if (name != null)
                return container.ResolveNamed<TContract>(name);

            return container.Resolve<TContract>();
        }

        public virtual IEnumerable<TContract> ResolveAll<TContract>(string name = null)
        {
            ILifetimeScope container = GetContainer();

            if (name != null)
                return container.ResolveNamed<IEnumerable<TContract>>(name);

            return container.Resolve<IEnumerable<TContract>>();
        }

        public virtual TContract ResolveOptional<TContract>(string name = null)
            where TContract : class
        {
            ILifetimeScope container = GetContainer();

            if (name != null)
                return container.ResolveNamed<TContract>(name);

            return container.ResolveOptional<TContract>();
        }

        public virtual object Resolve(TypeInfo contractType, string name = null)
        {
            if (contractType == null)
                throw new ArgumentNullException(nameof(contractType));

            ILifetimeScope container = GetContainer();

            if (name != null)
                return container.ResolveNamed(name, contractType);

            return container.Resolve(contractType);
        }

        public virtual object ResolveOptional(TypeInfo contractType, string name = null)
        {
            if (contractType == null)
                throw new ArgumentNullException(nameof(contractType));

            ILifetimeScope container = GetContainer();

            if (name != null)
                return container.ResolveNamed(name, contractType);

            return container.ResolveOptional(contractType);
        }

        public virtual IEnumerable<object> ResolveAll(TypeInfo contractType, string name = null)
        {
            if (contractType == null)
                throw new ArgumentNullException(nameof(contractType));

            ILifetimeScope container = GetContainer();

            if (name != null)
                return (IEnumerable<object>)container.ResolveNamed(name, contractType);

            return (IEnumerable<object>)container.Resolve(contractType);
        }

        public virtual object GetService(TypeInfo serviceType)
        {
            return GetContainer().ResolveOptional(serviceType);
        }

        public virtual object GetService(Type serviceType)
        {
            return GetContainer().ResolveOptional(serviceType);
        }

        public virtual IDependencyManager Register<TContract, TService>(string name = null,
            DependencyLifeCycle lifeCycle = DependencyLifeCycle.InstancePerLifetimeScope, bool overwriteExciting = true)
            where TService : class, TContract
        {
            return Register(typeof(TContract).GetTypeInfo(), typeof(TService).GetTypeInfo(), name, lifeCycle, overwriteExciting);
        }

        public virtual IDependencyManager RegisterInstance<TContract>(TContract obj, bool overwriteExciting = true, string name = null)
            where TContract : class
        {
            return RegisterInstance(obj, typeof(TContract).GetTypeInfo(), overwriteExciting, name);
        }

        public virtual IDependencyManager RegisterAssemblyTypes(Assembly[] assemblies, Predicate<TypeInfo> predicate = null)
        {
            GetContainerBuidler().RegisterAssemblyTypes(assemblies)
                .Where(t => predicate == null ? true : predicate(t.GetTypeInfo()))
                .PropertiesAutowired();

            return this;
        }
        public IDependencyManager RegisterGeneric(TypeInfo contractType, TypeInfo serviceType, DependencyLifeCycle lifeCycle)
        {
            IRegistrationBuilder<object, ReflectionActivatorData, DynamicRegistrationStyle> registration = GetContainerBuidler().RegisterGeneric(serviceType).PropertiesAutowired().As(contractType);

            if (lifeCycle == DependencyLifeCycle.SingleInstance)
                registration = registration.SingleInstance();
            else
                registration = registration.InstancePerLifetimeScope();

            return this;
        }

        public virtual IDependencyManager RegisterUsing<T>(Func<T> factory, string name = null,
            DependencyLifeCycle lifeCycle = DependencyLifeCycle.InstancePerLifetimeScope, bool overwriteExciting = true)
        {
            IRegistrationBuilder<T, SimpleActivatorData, SingleRegistrationStyle> registration = GetContainerBuidler().Register((context, parameter) => factory());

            if (overwriteExciting == false)
                registration = registration.PreserveExistingDefaults();

            if (name != null)
                registration = registration.Named<T>(name);

            if (lifeCycle == DependencyLifeCycle.SingleInstance)
                registration = registration.SingleInstance();
            else
                registration = registration.InstancePerLifetimeScope();

            return this;
        }

        public virtual IDependencyManager Register(TypeInfo contractType, TypeInfo serviceType, string name = null, DependencyLifeCycle lifeCycle = DependencyLifeCycle.InstancePerLifetimeScope, bool overwriteExciting = true)
        {
            if (serviceType == null)
                throw new ArgumentNullException(nameof(serviceType));

            if (contractType == null)
                throw new ArgumentNullException(nameof(contractType));

            var registration = GetContainerBuidler().RegisterType(serviceType)
                    .PropertiesAutowired()
                    .As(contractType);

            if (overwriteExciting == false)
                registration = registration.PreserveExistingDefaults();

            if (name != null)
                registration = registration.Named(name, contractType);

            if (lifeCycle == DependencyLifeCycle.SingleInstance)
                registration = registration.SingleInstance();
            else
                registration = registration.InstancePerLifetimeScope();

            return this;
        }

        public virtual IDependencyManager RegisterInstance(object obj, TypeInfo contractType, bool overwriteExciting = true, string name = null)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            var registration = GetContainerBuidler().RegisterInstance(obj).As(contractType);

            if (name != null)
                registration = registration.Named(name, contractType);

            if (overwriteExciting == false)
                registration = registration.PreserveExistingDefaults();

            return this;
        }

        public virtual bool IsRegistered<TContract>()
        {
            return GetContainer().IsRegistered<TContract>();
        }

        public virtual bool IsRegistered(TypeInfo contractType)
        {
            if (contractType == null)
                throw new ArgumentNullException(nameof(contractType));

            return GetContainer().IsRegistered(contractType);
        }

        public virtual void Dispose()
        {
            _container?.Dispose();
        }
    }
}
