using System;
using System.Collections.Generic;
using System.Reflection;
using Autofac;
using Autofac.Builder;
using Foundation.Core.Contracts;
using Autofac.Integration.SignalR;
using Autofac.Integration.WebApi;
using Foundation.Api.Contracts;

namespace Foundation.Api.Implementations
{
    public class AutofacDependencyManager : IDependencyManager, IAutofacDependencyManager
    {
        private ContainerBuilder _builder;

        private ILifetimeScope _container;

        private bool _isInited;

        public AutofacDependencyManager()
        {
        }

        public virtual IDependencyManager Init()
        {
            SetContainerBuilder(new ContainerBuilder());
            return this;
        }

        private IDependencyManager SetContainerBuilder(ContainerBuilder builder)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));
            if (_isInited == true)
                throw new InvalidOperationException("Container builder has been set already");
            _builder = builder;
            _builder.Register((context, parameter) => _container).SingleInstance();
            _builder.Register((context, parameter) => (IDependencyResolver)this).SingleInstance();
            _builder.Register((context, parameter) => (IDependencyManager)this).SingleInstance();
            _builder.Register((context, parameter) => (IServiceProvider)this).SingleInstance();
            _builder.Register((context, parameter) => (IAutofacDependencyManager)this).SingleInstance();
            _isInited = true;
            return this;
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
            SetContainer(_builder.Build());
            return this;
        }

        public virtual bool IsInited()
        {
            return _isInited != false;
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

        public virtual IDependencyManager Register<TContract, TService>(string name = null,
            DepepdencyLifeCycle lifeCycle = DepepdencyLifeCycle.InstancePerLifetimeScope, bool overwriteExciting = true)
            where TService : class, TContract
        {
            return Register(typeof(TContract).GetTypeInfo(), typeof(TService).GetTypeInfo(), name, lifeCycle, overwriteExciting);
        }

        public virtual IDependencyManager RegisterInstance<TContract>(TContract obj, bool overwriteExciting = true, string name = null)
            where TContract : class
        {
            return RegisterInstance(obj, typeof(TContract).GetTypeInfo(), overwriteExciting, name);
        }

        public virtual IDependencyManager RegisterHubs(params Assembly[] assemblies)
        {
            _builder.RegisterHubs(assemblies).SingleInstance();
            return this;
        }

        public virtual IDependencyManager RegisterApiControllers(params Assembly[] assemblies)
        {
            _builder.RegisterApiControllers(assemblies).PropertiesAutowired();
            return this;
        }

        public IDependencyManager RegisterGeneric(TypeInfo contractType, TypeInfo serviceType, DepepdencyLifeCycle lifeCycle)
        {
            IRegistrationBuilder<object, ReflectionActivatorData, DynamicRegistrationStyle> registration = GetContainerBuidler().RegisterGeneric(serviceType).As(contractType);

            if (lifeCycle == DepepdencyLifeCycle.SingleInstance)
                registration = registration.SingleInstance();
            else
                registration = registration.InstancePerLifetimeScope();

            return this;
        }

        public virtual object GetService(TypeInfo serviceType)
        {
            return _container.ResolveOptional(serviceType);
        }

        public virtual object GetService(Type serviceType)
        {
            return _container.ResolveOptional(serviceType);
        }

        public virtual IDependencyManager RegisterUsing<T>(Func<IDependencyResolver, T> factory, string name = null,
            DepepdencyLifeCycle lifeCycle = DepepdencyLifeCycle.InstancePerLifetimeScope, bool overwriteExciting = true)
        {
            IRegistrationBuilder<T, SimpleActivatorData, SingleRegistrationStyle> registration = _builder.Register((context, parameter) =>
            {
                IDependencyResolver resolver = context.Resolve<IDependencyResolver>();

                return factory(resolver);
            });

            if (overwriteExciting == false)
                registration = registration.PreserveExistingDefaults();

            if (name != null)
                registration = registration.Named<T>(name);

            if (lifeCycle == DepepdencyLifeCycle.SingleInstance)
                registration = registration.SingleInstance();
            else
                registration = registration.InstancePerLifetimeScope();

            return this;
        }

        public virtual IDependencyResolver CreateScope()
        {
            return new AutofacScopeBasedDependencyResolver(_container.BeginLifetimeScope());
        }

        public virtual void Dispose()
        {
            _container.Dispose();
        }

        public virtual ILifetimeScope GetContainer()
        {
            if (!IsInited())
                throw new InvalidOperationException("Container is not prepared, build it first.");

            return _container;
        }

        public virtual ContainerBuilder GetContainerBuidler()
        {
            return _builder;
        }

        public virtual IDependencyResolver CreateChildDependencyManager(Action<IDependencyManager> childDependencyManager)
        {
            if (childDependencyManager == null)
                throw new ArgumentNullException(nameof(childDependencyManager));

            AutofacDependencyManager childManager = null;

            ILifetimeScope scope = _container.BeginLifetimeScope(containerBuilder =>
            {
                childManager = new AutofacDependencyManager();

                childManager.SetContainerBuilder(containerBuilder);

                childDependencyManager(childManager);
            });

            childManager.SetContainer(scope);

            return childManager;
        }

        public virtual IDependencyManager Register(TypeInfo contractType, TypeInfo serviceType, string name = null, DepepdencyLifeCycle lifeCycle = DepepdencyLifeCycle.InstancePerLifetimeScope, bool overwriteExciting = true)
        {
            if (serviceType == null)
                throw new ArgumentNullException(nameof(serviceType));

            if (contractType == null)
                throw new ArgumentNullException(nameof(contractType));

            var registration = GetContainerBuidler().RegisterType(serviceType)
                    .As(contractType);

            if (overwriteExciting == false)
                registration = registration.PreserveExistingDefaults();

            if (name != null)
                registration = registration.Named(name, contractType);

            if (lifeCycle == DepepdencyLifeCycle.SingleInstance)
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
    }
}
