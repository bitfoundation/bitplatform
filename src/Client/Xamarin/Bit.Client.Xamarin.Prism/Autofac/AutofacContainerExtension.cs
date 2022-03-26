using Autofac;
using Autofac.Core;
using Bit.ViewModel;
using Bit.ViewModel.Contracts;
using Prism.Ioc;
using Prism.Navigation;
using Prism.Regions;
using Rg.Plugins.Popup.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Prism.Autofac
{
    public class AutofacScopeProvider : IScopedProvider
    {
        public AutofacScopeProvider(ILifetimeScope scope)
        {
            Scope = scope;
        }

        public virtual ILifetimeScope Scope { get; private set; }
        public virtual bool IsAttached { get; set; }
        public virtual IScopedProvider CurrentScope => this;
        public virtual IScopedProvider CreateScope() => this;

        public bool IsDisposed { get; private set; }

        public virtual void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (IsDisposed) return;
            Scope.Dispose();
            Scope = null!;
            IsDisposed = true;
        }

        ~AutofacScopeProvider()
        {
            Dispose(false);
        }

        public virtual object Resolve(Type type) =>
            Resolve(type, Array.Empty<(Type, object)>());

        public virtual object Resolve(Type type, string name) =>
            Resolve(type, name, Array.Empty<(Type, object)>());

        public virtual object Resolve(Type type, params (Type Type, object Instance)[] parameters) =>
            Resolve(type, name: null, parameters);

        public virtual object Resolve(Type type, string name, params (Type Type, object Instance)[] parameters)
        {
            try
            {
                var autofacParameters = PrepareParameters(Scope, parameters);

                return name != null ? Scope.ResolveNamed(name, type, autofacParameters) : Scope.Resolve(type, autofacParameters);
            }
            catch (Exception ex)
            {
                throw new ContainerResolutionException(type, name, ex);
            }
        }

        internal static List<Parameter> PrepareParameters(ILifetimeScope scope, (Type Type, object Instance)[] parameters)
        {
            List<Parameter> result = parameters.Select(p => new TypedParameter(p.Type, p.Instance) { }).Cast<Parameter>().ToList();

            INavigationService prismNavService = result.OfType<TypedParameter>().Select(r => r.Value).OfType<INavigationService>().SingleOrDefault();

            if (prismNavService != null)
            {
                INavService navService = BuildNavService(scope, prismNavService);

                // ctor
                result.Add(new TypedParameter(typeof(INavService), navService));

                // prop
                result.Add(new NamedPropertyParameter(nameof(INavService.PrismNavigationService), prismNavService));
                result.Add(new NamedPropertyParameter(nameof(BitViewModelBase.NavigationService), navService));
            }

            return result;
        }

        internal static INavService BuildNavService(ILifetimeScope scope, INavigationService prismNavigationService)
        {
            return scope!.Resolve<INavServiceFactory>()(prismNavigationService, scope!.Resolve<IPopupNavigation>(), scope.Resolve<IRegionManager>(), scope.Resolve<AnimateNavigation>());
        }
    }

    public class AutofacContainerExtension : IAutofacContainerExtension
    {
        private AutofacScopeProvider _currentScope;
        private AutofacScopeProvider _rootScope;

        public virtual IContainer Instance { get; protected set; }

        public virtual ContainerBuilder Builder { get; set; }

        public AutofacContainerExtension()
            : this(new ContainerBuilder())
        {
        }

        public AutofacContainerExtension(ContainerBuilder builder)
        {
            Builder = builder;
            string currentContainer = "CurrentContainer";
            Builder.RegisterInstance(this).Named(currentContainer, typeof(AutofacContainerExtension));
            Builder.Register(c => c.ResolveNamed<AutofacContainerExtension>(currentContainer)).As(typeof(IContainerExtension));
            Builder.Register(c => c.ResolveNamed<AutofacContainerExtension>(currentContainer)).As(typeof(IContainerProvider));
            ExceptionExtensions.RegisterFrameworkExceptionType(typeof(DependencyResolutionException));
        }

        public virtual IScopedProvider CurrentScope => _currentScope;

        public virtual void FinalizeExtension()
        {
            if (Instance == null)
            {
                Instance = Builder.Build();
                _rootScope = new AutofacScopeProvider(Instance);
            }
        }

        public virtual IContainerRegistry RegisterInstance(Type type, object instance)
        {
            Builder.RegisterInstance(instance).As(type);
            return this;
        }

        public virtual IContainerRegistry RegisterInstance(Type type, object instance, string name)
        {
            Builder.RegisterInstance(instance).Named(name, type);
            return this;
        }

        public virtual IContainerRegistry RegisterSingleton(Type from, Type to)
        {
            Builder.RegisterType(to).As(from).SingleInstance();
            return this;
        }

        public virtual IContainerRegistry RegisterSingleton(Type from, Type to, string name)
        {
            Builder.RegisterType(to).Named(name, from).SingleInstance();
            return this;
        }

        public virtual IContainerRegistry RegisterSingleton(Type type, Func<object> factoryMethod)
        {
            Builder.Register(c => factoryMethod()).SingleInstance().As(type);
            return this;
        }

        public virtual IContainerRegistry RegisterSingleton(Type type, Func<IContainerProvider, object> factoryMethod)
        {
            Builder.Register(c => factoryMethod(c.Resolve<IContainerProvider>())).As(type).SingleInstance();
            return this;
        }

        public virtual IContainerRegistry RegisterManySingleton(Type type, params Type[] serviceTypes)
        {
            Builder.RegisterType(type).As(serviceTypes).SingleInstance();
            return this;
        }

        public IContainerRegistry Register(Type from, Type to)
        {
            Builder.RegisterType(to).As(from);
            return this;
        }

        public IContainerRegistry Register(Type from, Type to, string name)
        {
            Builder.RegisterType(to).Named(name, from);
            return this;
        }

        public IContainerRegistry Register(Type type, Func<object> factoryMethod)
        {
            Builder.Register(c => factoryMethod()).As(type);
            return this;
        }

        public IContainerRegistry Register(Type type, Func<IContainerProvider, object> factoryMethod)
        {
            Builder.Register(c => factoryMethod(c.Resolve<IContainerProvider>())).As(type);
            return this;
        }

        public IContainerRegistry RegisterMany(Type type, params Type[] serviceTypes)
        {
            Builder.RegisterType(type).As(serviceTypes);
            return this;
        }

        public IContainerRegistry RegisterScoped(Type from, Type to)
        {
            Builder.RegisterType(to).As(from).InstancePerLifetimeScope();
            return this;
        }

        public IContainerRegistry RegisterScoped(Type type, Func<object> factoryMethod)
        {
            Builder.Register(c => factoryMethod()).InstancePerLifetimeScope();
            return this;
        }

        public IContainerRegistry RegisterScoped(Type type, Func<IContainerProvider, object> factoryMethod)
        {
            Builder.Register(c => factoryMethod(c.Resolve<IContainerProvider>())).As(type).InstancePerLifetimeScope();
            return this;
        }

        public object Resolve(Type type) =>
            Resolve(type, Array.Empty<(Type, object)>());

        public object Resolve(Type type, string name) =>
            Resolve(type, name, Array.Empty<(Type, object)>());

        public object Resolve(Type type, params (Type Type, object Instance)[] parameters) =>
            Resolve(type, name: null, parameters);

        public object Resolve(Type type, string name, params (Type Type, object Instance)[] parameters)
        {
            return (_currentScope is not null && _currentScope.IsDisposed == false ? _currentScope : _rootScope).Resolve(type, name, parameters);
        }

        public bool IsRegistered(Type type)
        {
            return Instance?.IsRegistered(type) == true; // workaround for prism's rg plugin popup
        }

        public bool IsRegistered(Type type, string name)
        {
            return Instance.IsRegisteredWithName(name, type);
        }

        public virtual IScopedProvider CreateScope() =>
            CreateScopeInternal();

        protected IScopedProvider CreateScopeInternal()
        {
            var child = Instance.BeginLifetimeScope();
            _currentScope = new AutofacScopeProvider(child);
            return _currentScope;
        }
    }
}
