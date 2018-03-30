using Autofac;
using Bit.ViewModel.Contracts;
using Bit.ViewModel.Implementations;
using IdentityModel.Client;
using Plugin.Connectivity;
using Plugin.Connectivity.Abstractions;
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

            containerRegistry.RegisterSingleton<IDateTimeProvider, DefaultDateTimeProvider>();
            containerRegistry.GetBuilder().Register<IConnectivity>(c => CrossConnectivity.Current).SingleInstance();

            return containerRegistry;
        }

        public static IContainerRegistry RegisterIdentityClient(this IContainerRegistry containerRegistry)
        {
            if (containerRegistry == null)
                throw new ArgumentNullException(nameof(containerRegistry));

            containerRegistry.RegisterSingleton<ISecurityService, DefaultSecurityService>();
            containerRegistry.GetBuilder().Register(c => AccountStore.Create()).SingleInstance();

            containerRegistry.GetBuilder().Register<TokenClient>((c, parameters) =>
            {
                return new TokenClient(address: new Uri(c.Resolve<IClientAppProfile>().HostUri, "core/connect/token").ToString(), clientId: parameters.Named<string>("clientId"), clientSecret: parameters.Named<string>("secret"), innerHttpMessageHandler: c.ResolveNamed<HttpMessageHandler>(ContractKeys.DefaultHttpMessageHandler));
            });

            return containerRegistry;
        }

        public static IContainerRegistry RegisterHttpClient<THttpMessageHandler>(this IContainerRegistry containerRegistry)
            where THttpMessageHandler : HttpMessageHandler, new()
        {
            if (containerRegistry == null)
                throw new ArgumentNullException(nameof(containerRegistry));

            containerRegistry.GetBuilder()
                .RegisterType<THttpMessageHandler>()
                .Named<HttpMessageHandler>(ContractKeys.DefaultHttpMessageHandler)
                .SingleInstance();

            containerRegistry.GetBuilder().Register<HttpMessageHandler>(c =>
            {
                return new AuthenticatedHttpMessageHandler(c.Resolve<IEventAggregator>(), c.Resolve<ISecurityService>(), c.ResolveNamed<HttpMessageHandler>(ContractKeys.DefaultHttpMessageHandler));
            })
            .Named<HttpMessageHandler>(ContractKeys.AuthenticatedHttpMessageHandler)
            .SingleInstance();

            containerRegistry.GetBuilder().Register<HttpClient>(c =>
            {
                HttpMessageHandler authenticatedHttpMessageHandler = c.ResolveNamed<HttpMessageHandler>(ContractKeys.AuthenticatedHttpMessageHandler);
                HttpClient httpClient = new HttpClient(authenticatedHttpMessageHandler) { BaseAddress = c.Resolve<IClientAppProfile>().HostUri };
                return httpClient;
            }).SingleInstance();

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

            containerRegistry.GetBuilder().Register<IODataClient>(c =>
            {
                HttpMessageHandler authenticatedHttpMessageHandler = c.ResolveNamed<HttpMessageHandler>(ContractKeys.AuthenticatedHttpMessageHandler);

                IClientAppProfile clientAppProfile = c.Resolve<IClientAppProfile>();

                IODataClient odataClient = new ODataClient(new ODataClientSettings(new Uri(clientAppProfile.HostUri, clientAppProfile.ODataRoute))
                {
                    RenewHttpConnection = false,
                    OnCreateMessageHandler = () => authenticatedHttpMessageHandler
                });

                return odataClient;
            });

            containerRegistry.GetBuilder()
                .Register<ODataBatch>(c => new ODataBatch(c.Resolve<IODataClient>(), reuseSession: true));

            return containerRegistry;
        }
    }
}
