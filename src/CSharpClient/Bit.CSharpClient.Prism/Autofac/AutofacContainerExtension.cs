using Autofac;
using Autofac.Core;
using Bit.ViewModel;
using Bit.ViewModel.Contracts;
using Prism.Ioc;
using Prism.Navigation;
using Rg.Plugins.Popup.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Prism.Autofac
{
    public class AutofacContainerExtension : IAutofacContainerExtension
    {
        public ContainerBuilder Builder { get; }

        public IContainer? Instance { get; private set; }

        public AutofacContainerExtension(ContainerBuilder builder)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            Builder = builder;
        }

        public void FinalizeExtension()
        {
            Instance = Builder.Build();
        }

        public IContainerRegistry RegisterInstance(Type type, object instance, string name)
        {
            Builder.RegisterInstance(instance).Named(name, serviceType: type);
            return this;
        }

        public IContainerRegistry RegisterInstance(Type type, object instance)
        {
            Builder.RegisterInstance(instance).As(type);
            return this;
        }

        public IContainerRegistry RegisterSingleton(Type from, Type to, string name)
        {
            Builder.RegisterType(to).SingleInstance().Named(name, to);
            return this;
        }

        public IContainerRegistry RegisterSingleton(Type from, Type to)
        {
            Builder.RegisterType(to).As(from).SingleInstance();
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

        public bool IsRegistered(Type type)
        {
            return Instance?.IsRegistered(type) == true; // workaround for prism's rg plugin popup
        }

        public bool IsRegistered(Type type, string name)
        {
            return Instance!.IsRegisteredWithName(name, type) == true;
        }

        public object Resolve(Type type)
        {
            return Instance!.Resolve(type);
        }

        public object Resolve(Type type, string name)
        {
            return Instance!.ResolveNamed(name, type);
        }

        public object Resolve(Type type, params (Type Type, object Instance)[] parameters)
        {
            return Instance!.Resolve(type, PrepareParameters(parameters));
        }

        List<Parameter> PrepareParameters((Type Type, object Instance)[] parameters)
        {
            var result = parameters.Select(p => new TypedParameter(p.Type, p.Instance) { }).Cast<Parameter>().ToList();

            var prismNavService = result.OfType<TypedParameter>().Select(r => r.Value).OfType<INavigationService>().SingleOrDefault();

            if (prismNavService != null)
            {
                INavService navService = BuildNavService(prismNavService);

                // ctor
                result.Add(new TypedParameter(typeof(INavService), navService));

                // prop
                result.Add(new NamedPropertyParameter(nameof(INavService.PrismNavigationService), prismNavService));
                result.Add(new NamedPropertyParameter(nameof(BitViewModelBase.NavigationService), navService));
            }

            return result;
        }

        public object Resolve(Type type, string name, params (Type Type, object Instance)[] parameters)
        {
            return Instance!.ResolveNamed(name, type, PrepareParameters(parameters));
        }

        public virtual INavService BuildNavService(INavigationService prismNavigationService)
        {
            return Instance!.Resolve<INavServiceFactory>()(prismNavigationService, Instance!.Resolve<IPopupNavigation>());
        }
    }
}
