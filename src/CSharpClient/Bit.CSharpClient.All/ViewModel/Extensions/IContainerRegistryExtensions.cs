using Autofac;
using Bit.ViewModel;
using Bit.ViewModel.Contracts;
using Bit.ViewModel.Implementations;
using IdentityModel.Client;
using Prism.Autofac;
using Prism.Events;
using Rg.Plugins.Popup.Contracts;
using Rg.Plugins.Popup.Pages;
using Rg.Plugins.Popup.Services;
using Simple.OData.Client;
using System;
using System.Net.Http;
using Xamarin.Auth;

namespace Prism.Ioc
{
    public static class IContainerRegistryExtensions
    {
        public static IContainerRegistry RegisterRequiredServices(this IContainerRegistry containerRegistry)
        {
            if (containerRegistry == null)
                throw new ArgumentNullException(nameof(containerRegistry));

            ContainerBuilder containerBuilder = containerRegistry.GetBuilder();

            containerBuilder.RegisterType<DefaultDateTimeProvider>().As<IDateTimeProvider>().SingleInstance().PreserveExistingDefaults();

            return containerRegistry;
        }

        public static IContainerRegistry RegisterIdentityClient(this IContainerRegistry containerRegistry)
        {
            if (containerRegistry == null)
                throw new ArgumentNullException(nameof(containerRegistry));

            ContainerBuilder containerBuilder = containerRegistry.GetBuilder();

            containerBuilder.RegisterType<DefaultSecurityService>().As<ISecurityService>().SingleInstance().PreserveExistingDefaults();

#if Android
            containerBuilder.Register(c => AccountStore.Create(c.Resolve<Android.Content.Context>(), c.Resolve<IClientAppProfile>().AppName)).SingleInstance().PreserveExistingDefaults();
#else
            containerBuilder.Register(c => AccountStore.Create()).SingleInstance().PreserveExistingDefaults();
#endif

            containerBuilder.Register((c, parameters) =>
            {
                return new TokenClient(address: new Uri(c.Resolve<IClientAppProfile>().HostUri, "core/connect/token").ToString(), clientId: parameters.Named<string>("clientId"), clientSecret: parameters.Named<string>("secret"), innerHttpMessageHandler: c.ResolveNamed<HttpMessageHandler>(ContractKeys.DefaultHttpMessageHandler));
            }).PreserveExistingDefaults();

            return containerRegistry;
        }

        public static IContainerRegistry RegisterHttpClient<THttpMessageHandler>(this IContainerRegistry containerRegistry)
            where THttpMessageHandler : HttpMessageHandler, new()
        {
            if (containerRegistry == null)
                throw new ArgumentNullException(nameof(containerRegistry));

            ContainerBuilder containerBuilder = containerRegistry.GetBuilder();

            containerBuilder
                .RegisterType<THttpMessageHandler>()
                .Named<HttpMessageHandler>(ContractKeys.DefaultHttpMessageHandler)
                .SingleInstance()
                .PreserveExistingDefaults();

            containerBuilder.Register<HttpMessageHandler>(c =>
            {
                return new AuthenticatedHttpMessageHandler(c.Resolve<IEventAggregator>(), c.Resolve<ISecurityService>(), c.ResolveNamed<HttpMessageHandler>(ContractKeys.DefaultHttpMessageHandler));
            })
            .Named<HttpMessageHandler>(ContractKeys.AuthenticatedHttpMessageHandler)
            .SingleInstance()
            .PreserveExistingDefaults();

            containerBuilder.Register(c =>
            {
                HttpMessageHandler authenticatedHttpMessageHandler = c.ResolveNamed<HttpMessageHandler>(ContractKeys.AuthenticatedHttpMessageHandler);
                HttpClient httpClient = new HttpClient(authenticatedHttpMessageHandler) { BaseAddress = c.Resolve<IClientAppProfile>().HostUri };
                return httpClient;
            }).SingleInstance()
            .PreserveExistingDefaults();

            return containerRegistry;
        }

        public static IContainerRegistry RegisterHttpClient(this IContainerRegistry containerRegistry)
        {
            if (containerRegistry == null)
                throw new ArgumentNullException(nameof(containerRegistry));

            return RegisterHttpClient<BitHttpClientHandler>(containerRegistry);
        }

        public static IContainerRegistry RegisterODataClient(this IContainerRegistry containerRegistry)
        {
            if (containerRegistry == null)
                throw new ArgumentNullException(nameof(containerRegistry));

            Simple.OData.Client.V4Adapter.Reference();

            ContainerBuilder containerBuilder = containerRegistry.GetBuilder();

            containerBuilder.Register(c =>
            {
                HttpMessageHandler authenticatedHttpMessageHandler = c.ResolveNamed<HttpMessageHandler>(ContractKeys.AuthenticatedHttpMessageHandler);

                IClientAppProfile clientAppProfile = c.Resolve<IClientAppProfile>();

                IODataClient odataClient = new ODataClient(new ODataClientSettings(new Uri(clientAppProfile.HostUri, clientAppProfile.ODataRoute))
                {
                    RenewHttpConnection = false,
                    OnCreateMessageHandler = () => authenticatedHttpMessageHandler
                });

                return odataClient;
            }).PreserveExistingDefaults();

            containerBuilder
                .Register(c => new ODataBatch(c.Resolve<IODataClient>(), reuseSession: true))
                .PreserveExistingDefaults();

            return containerRegistry;
        }

        public static IContainerRegistry RegisterPopupService(this IContainerRegistry containerRegistry)
        {
            if (containerRegistry == null)
                throw new ArgumentNullException(nameof(containerRegistry));

            ContainerBuilder containerBuilder = containerRegistry.GetBuilder();

            containerBuilder
                .Register<IPopupNavigation>(c => PopupNavigation.Instance)
                .PreserveExistingDefaults();

            containerBuilder.RegisterType<DefaultPopupNavigationService>().As<IPopupNavigationService>().SingleInstance().PreserveExistingDefaults();

            return containerRegistry;
        }

        public static IContainerRegistry RegisterForPopup<TView, TViewModel>(this IContainerRegistry containerRegistry, string name)
            where TView : PopupPage
            where TViewModel : BitViewModelBase
        {
            if (containerRegistry == null)
                throw new ArgumentNullException(nameof(containerRegistry));

            containerRegistry.Register<TViewModel>();
            containerRegistry.Register<PopupPage, TView>(name);

            return containerRegistry;
        }
    }
}
