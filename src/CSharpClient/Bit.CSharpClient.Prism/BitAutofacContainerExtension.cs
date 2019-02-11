using Autofac;
using Autofac.Core;
using Bit.ViewModel;
using Bit.ViewModel.Contracts;
using Bit.ViewModel.Implementations;
using Prism.Autofac;
using Prism.Ioc;
using Prism.Navigation;
using Rg.Plugins.Popup.Services;
using System;
using System.Collections.Generic;
using Xamarin.Forms;

namespace Bit
{
    public class BitAutofacContainerExtension : IAutofacContainerExtension
    {
        public ContainerBuilder Builder { get; }

        public IContainer Instance { get; private set; }

        public bool SupportsModules => false;

        public BitAutofacContainerExtension(ContainerBuilder builder)
        {
            Builder = builder;
        }

        public void FinalizeExtension()
        {
            Instance = Builder.Build();
        }

        public void RegisterInstance(Type type, object instance)
        {
            Builder.RegisterInstance(instance).As(type);
        }

        public void RegisterSingleton(Type from, Type to)
        {
            Builder.RegisterType(to).As(from).SingleInstance();
        }

        public void Register(Type from, Type to)
        {
            Builder.RegisterType(to).As(from);
        }

        public void Register(Type from, Type to, string name)
        {
            Builder.RegisterType(to).Named(name, from);
        }

        public object Resolve(Type type)
        {
            return Instance.Resolve(type);
        }

        public object Resolve(Type type, string name)
        {
            return Instance.ResolveNamed(name, type);
        }

        public object ResolveViewModelForView(object view, Type viewModelType)
        {
            List<Parameter> parameters = new List<Parameter> { };

            if (view is Page page)
            {
                INavigationService prismNavigationService = this.CreateNavigationService(page);

                INavService navService = BuildNavService(prismNavigationService);

                // ctor
                parameters.Add(new TypedParameter(typeof(INavigationService), prismNavigationService));
                parameters.Add(new TypedParameter(typeof(INavService), navService));

                // prop
                parameters.Add(new NamedPropertyParameter(nameof(INavService.PrismNavigationService), prismNavigationService));
                parameters.Add(new NamedPropertyParameter(nameof(BitViewModelBase.NavigationService), navService));
            }

            return Instance.Resolve(viewModelType, parameters: parameters);
        }

        public virtual INavService BuildNavService(INavigationService prismNavigationService)
        {
            return new DefaultNavService
            {
                PrismNavigationService = prismNavigationService,
                PopupNavigation = PopupNavigation.Instance
            };
        }
    }
}
