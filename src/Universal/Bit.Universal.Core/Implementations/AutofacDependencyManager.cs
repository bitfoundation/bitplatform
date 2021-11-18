using Autofac;
using Autofac.Builder;
using Autofac.Extensions.DependencyInjection;
using Bit.Core.Contracts;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Bit.Core.Implementations
{
    public class AutofacDependencyManager : IDependencyManager, IAutofacDependencyManager, IServiceCollectionAccessor, IServiceProviderFactory<IDependencyManager>
    {
        private ContainerBuilder? _containerBuilder;
        private ILifetimeScope? _container;

        IServiceCollection IServiceCollectionAccessor.ServiceCollection { get; set; } = default!;

        public virtual IDependencyManager Init()
        {
            UseContainerBuilder(new ContainerBuilder());
            return this;
        }

        public virtual void UseContainerBuilder(ContainerBuilder builder)
        {
            if (_containerBuilder != null)
                throw new InvalidOperationException("Container builder has been set already");

            _containerBuilder = builder ?? throw new ArgumentNullException(nameof(builder));
        }

        private IDependencyManager SetContainer(ILifetimeScope container)
        {
            if (_container != null)
                throw new InvalidOperationException("Container has been set already");
            _container = container ?? throw new ArgumentNullException(nameof(container));
            return this;
        }

        public virtual IDependencyManager BuildContainer()
        {
            if (!IsInited())
                throw new InvalidOperationException("Container builder is not prepared. Call init first.");
            SetContainer((_containerBuilder!).Build());
            return this;
        }

        public virtual bool IsInited()
        {
            return _containerBuilder != null;
        }

        public virtual bool ContainerIsBuilt()
        {
            return _container != null;
        }

        public virtual void UseContainer(ILifetimeScope lifetimeScope)
        {
            SetContainer(lifetimeScope);
        }

        public virtual ILifetimeScope GetContainer()
        {
            if (!ContainerIsBuilt())
                throw new InvalidOperationException("Container is not built. Call build first.");

            return _container!;
        }

        public virtual ContainerBuilder GetContainerBuidler()
        {
            if (_containerBuilder == null)
                throw new InvalidOperationException("Container builder is not prepared, Either call Init or UseContainerBuilder first");

            return _containerBuilder;
        }

        public virtual IDependencyResolver CreateChildDependencyResolver(Action<IDependencyManager>? childDependencyManagerCustomizer = null)
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

        public virtual TService Resolve<TService>(string? name = null)
            where TService : notnull
        {
            ILifetimeScope container = GetContainer();

            if (name != null)
                return container.ResolveNamed<TService>(name);

            return container.Resolve<TService>();
        }

        public virtual IEnumerable<TService> ResolveAll<TService>(string? name = null)
            where TService : notnull
        {
            ILifetimeScope container = GetContainer();

            if (name != null)
                return container.ResolveNamed<IEnumerable<TService>>(name);

            return container.Resolve<IEnumerable<TService>>();
        }

        public virtual TService? ResolveOptional<TService>(string? name = null)
            where TService : class
        {
            ILifetimeScope container = GetContainer();

            if (name != null)
                return container.ResolveNamed<TService>(name);

            return container.ResolveOptional<TService>();
        }

        public virtual object Resolve(TypeInfo serviceType, string? name = null)
        {
            if (serviceType == null)
                throw new ArgumentNullException(nameof(serviceType));

            ILifetimeScope container = GetContainer();

            if (name != null)
                return container.ResolveNamed(name, serviceType);

            return container.Resolve(serviceType);
        }

        public virtual object? ResolveOptional(TypeInfo serviceType, string? name = null)
        {
            if (serviceType == null)
                throw new ArgumentNullException(nameof(serviceType));

            ILifetimeScope container = GetContainer();

            if (name != null)
                return container.ResolveNamed(name, serviceType);

            return container.ResolveOptional(serviceType);
        }

        public virtual IEnumerable<object> ResolveAll(TypeInfo serviceType, string? name = null)
        {
            if (serviceType == null)
                throw new ArgumentNullException(nameof(serviceType));

            ILifetimeScope container = GetContainer();

            if (name != null)
                return (IEnumerable<object>)container.ResolveNamed(name, serviceType);

            return (IEnumerable<object>)container.Resolve(serviceType);
        }

        public virtual object? GetService(TypeInfo serviceType)
        {
            return GetContainer().ResolveOptional(serviceType);
        }

        public virtual object? GetService(Type serviceType)
        {
            return GetContainer().ResolveOptional(serviceType);
        }

        public virtual IDependencyManager Register<T>(string? name = null,
            DependencyLifeCycle lifeCycle = DependencyLifeCycle.PerScopeInstance, bool overwriteExisting = true)
            where T : class
        {
            return Register<T, T>(name, lifeCycle, overwriteExisting);
        }

        public virtual IDependencyManager Register<TService, TImplementation>(string? name = null,
            DependencyLifeCycle lifeCycle = DependencyLifeCycle.PerScopeInstance, bool overwriteExisting = true)
            where TImplementation : class, TService
        {
            return Register(new[] { typeof(TService).GetTypeInfo() }, typeof(TImplementation).GetTypeInfo(), name, lifeCycle, overwriteExisting);
        }

        public virtual IDependencyManager RegisterInstance<TService>(TService implementationInstance, bool overwriteExisting = true, string? name = null)
            where TService : class
        {
            return RegisterInstance(implementationInstance, new[] { typeof(TService).GetTypeInfo() }, overwriteExisting, name);
        }

        public virtual IDependencyManager RegisterAssemblyTypes(Assembly[] assemblies, Predicate<TypeInfo>? predicate = null, DependencyLifeCycle lifeCycle = DependencyLifeCycle.PerScopeInstance)
        {
            IRegistrationBuilder<object, ReflectionActivatorData, DynamicRegistrationStyle> registration = GetContainerBuidler().RegisterAssemblyTypes(assemblies)
                .Where(t => predicate == null || predicate(t.GetTypeInfo()))
                .PropertiesAutowired(wiringFlags: PropertyWiringOptions.PreserveSetValues);

            if (lifeCycle == DependencyLifeCycle.Transient)
                registration = registration.InstancePerDependency();
            else if (lifeCycle == DependencyLifeCycle.SingleInstance)
                registration = registration.SingleInstance();
            else
                registration = registration.InstancePerLifetimeScope();

            return this;
        }

        /// <summary>
        /// Register an unparameterized generic type, e.g. IRepository&lt;&gt;. Concrete types will be made as they are requested, e.g. with IRepository&lt;Customer&gt;
        /// </summary>
        public virtual IDependencyManager RegisterGeneric(TypeInfo serviceType, TypeInfo implementationType, DependencyLifeCycle lifeCycle = DependencyLifeCycle.PerScopeInstance)
        {
            return RegisterGeneric(new[] { serviceType }, implementationType, lifeCycle);
        }

        public virtual IDependencyManager RegisterGeneric(TypeInfo[] servicesType, TypeInfo implementationType, DependencyLifeCycle lifeCycle = DependencyLifeCycle.PerScopeInstance)
        {
            IRegistrationBuilder<object, ReflectionActivatorData, DynamicRegistrationStyle> registration = GetContainerBuidler().RegisterGeneric(implementationType)
                .PropertiesAutowired(wiringFlags: PropertyWiringOptions.PreserveSetValues)
                .As(servicesType);

            if (lifeCycle == DependencyLifeCycle.Transient)
                registration = registration.InstancePerDependency();
            else if (lifeCycle == DependencyLifeCycle.SingleInstance)
                registration = registration.SingleInstance();
            else
                registration = registration.InstancePerLifetimeScope();

            return this;
        }

        public virtual IDependencyManager RegisterUsing<T>(Func<IDependencyResolver, T> factory, string? name = null,
            DependencyLifeCycle lifeCycle = DependencyLifeCycle.PerScopeInstance, bool overwriteExisting = true)
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            return RegisterUsing((depManager) => factory(depManager)!, new[] { typeof(T).GetTypeInfo() }, name, lifeCycle, overwriteExisting);
        }

        public virtual IDependencyManager RegisterUsing(Func<IDependencyResolver, object> factory, TypeInfo serviceType, string? name = null, DependencyLifeCycle lifeCycle = DependencyLifeCycle.PerScopeInstance, bool overwriteExisting = true)
        {
            return RegisterUsing(factory, new[] { serviceType }, name, lifeCycle, overwriteExisting);
        }

        public virtual IDependencyManager RegisterUsing(Func<IDependencyResolver, object> factory, TypeInfo[] servicesType, string? name = null, DependencyLifeCycle lifeCycle = DependencyLifeCycle.PerScopeInstance, bool overwriteExisting = true)
        {
            if (servicesType == null)
                throw new ArgumentNullException(nameof(servicesType));

            IRegistrationBuilder<object, SimpleActivatorData, SingleRegistrationStyle> registration = GetContainerBuidler().Register((context, parameter) =>
            {
                AutofacDependencyManager currentAutofacDepdencyManager = new AutofacDependencyManager();
                currentAutofacDepdencyManager.UseContainer(context.Resolve<ILifetimeScope>());
                return factory.DynamicInvoke(currentAutofacDepdencyManager)!;
            }).As(servicesType);

            if (overwriteExisting == false)
                registration = registration.PreserveExistingDefaults();

            if (name != null)
            {
                foreach (TypeInfo serviceType in servicesType)
                    registration = registration.Named(name, serviceType);
            }

            if (lifeCycle == DependencyLifeCycle.Transient)
                registration = registration.InstancePerDependency();
            else if (lifeCycle == DependencyLifeCycle.SingleInstance)
                registration = registration.SingleInstance();
            else
                registration = registration.InstancePerLifetimeScope();

            return this;
        }

        public virtual IDependencyManager Register(TypeInfo serviceType, TypeInfo implementationType, string? name = null, DependencyLifeCycle lifeCycle = DependencyLifeCycle.PerScopeInstance, bool overwriteExisting = true)
        {
            return Register(new[] { serviceType }, implementationType, name, lifeCycle, overwriteExisting);
        }

        public virtual IDependencyManager Register(TypeInfo[] servicesType, TypeInfo implementationType, string? name = null, DependencyLifeCycle lifeCycle = DependencyLifeCycle.PerScopeInstance, bool overwriteExisting = true)
        {
            if (implementationType == null)
                throw new ArgumentNullException(nameof(implementationType));

            if (servicesType == null)
                throw new ArgumentNullException(nameof(servicesType));

            IRegistrationBuilder<object, ConcreteReflectionActivatorData, SingleRegistrationStyle> registration = GetContainerBuidler().RegisterType(implementationType)
                    .PropertiesAutowired(wiringFlags: PropertyWiringOptions.PreserveSetValues)
                    .As(servicesType);

            if (overwriteExisting == false)
                registration = registration.PreserveExistingDefaults();

            if (name != null)
            {
                foreach (TypeInfo serviceType in servicesType)
                    registration = registration.Named(name, serviceType);
            }

            if (lifeCycle == DependencyLifeCycle.Transient)
                registration = registration.InstancePerDependency();
            else if (lifeCycle == DependencyLifeCycle.SingleInstance)
                registration = registration.SingleInstance();
            else
                registration = registration.InstancePerLifetimeScope();

            return this;
        }

        public virtual IDependencyManager RegisterInstance(object obj, TypeInfo serviceType, bool overwriteExisting = true, string? name = null)
        {
            return RegisterInstance(obj, new[] { serviceType }, overwriteExisting, name);
        }

        public virtual IDependencyManager RegisterInstance(object obj, TypeInfo[] servicesType, bool overwriteExisting = true, string? name = null)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            if (servicesType == null)
                throw new ArgumentNullException(nameof(servicesType));

            IRegistrationBuilder<object, SimpleActivatorData, SingleRegistrationStyle> registration = GetContainerBuidler().RegisterInstance(obj).As(servicesType);

            if (name != null)
            {
                foreach (TypeInfo serviceType in servicesType)
                    registration = registration.Named(name, serviceType);
            }

            if (overwriteExisting == false)
                registration = registration.PreserveExistingDefaults();

            return this;
        }

        public virtual bool IsRegistered<TService>()
            where TService : notnull
        {
            return GetContainer().IsRegistered<TService>();
        }

        public virtual bool IsRegistered(TypeInfo serviceType)
        {
            if (serviceType == null)
                throw new ArgumentNullException(nameof(serviceType));

            return GetContainer().IsRegistered(serviceType);
        }

        private bool isDisposed;

        public virtual void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (isDisposed) return;
            _container?.Dispose();
            isDisposed = true;
        }

        public virtual async ValueTask DisposeAsync()
        {
            if (isDisposed) return;
            if (_container != null)
                await _container.DisposeAsync().ConfigureAwait(false);
            isDisposed = true;
        }

        public virtual IDependencyManager Populate(IServiceCollection services)
        {
            GetContainerBuidler().Populate(services);
            return this;
        }

        public virtual IDependencyManager CreateBuilder(IServiceCollection services)
        {
            Populate(services);

            return this;
        }

        public virtual IServiceProvider CreateServiceProvider(IDependencyManager dependencyManager)
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            dependencyManager.BuildContainer();

            return dependencyManager;
        }

        private static readonly Type _disposerType = Assembly.Load("Autofac").GetType("Autofac.Core.Disposer");
        private static readonly FieldInfo _itemsField = _disposerType.GetField("_items", BindingFlags.NonPublic | BindingFlags.Instance);

        public IPipelineAwareDisposable[] GetPipelineAwareDisposables()
        {
            var disposer = GetContainer()
                .Disposer;

            var objects = (Stack<object>)_itemsField.GetValue(disposer);

            return objects.OfType<IPipelineAwareDisposable>().ToArray();
        }
    }
}
