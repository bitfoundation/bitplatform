using Autofac;
using Bit.ViewModel.Contracts;
using Bit.ViewModel.Implementations;
using IdentityModel.Client;
using Plugin.Connectivity;
using Prism.Autofac;
using Prism.Events;
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
            containerBuilder.Register(c => CrossConnectivity.Current).SingleInstance().PreserveExistingDefaults();

            return containerRegistry;
        }

        public static IContainerRegistry RegisterIdentityClient(this IContainerRegistry containerRegistry)
        {
            if (containerRegistry == null)
                throw new ArgumentNullException(nameof(containerRegistry));

            ContainerBuilder containerBuilder = containerRegistry.GetBuilder();

            containerBuilder.RegisterType<DefaultSecurityService>().As<ISecurityService>().SingleInstance().PreserveExistingDefaults();

            containerBuilder.Register(c => AccountStore.Create()).SingleInstance().PreserveExistingDefaults();

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

            return RegisterHttpClient<HttpClientHandler>(containerRegistry);
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
    }
}
