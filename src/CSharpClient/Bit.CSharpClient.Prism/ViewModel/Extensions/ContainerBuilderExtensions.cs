using Autofac;
using Bit.ViewModel.Contracts;
using Bit.ViewModel.Implementations;
using System;

namespace Prism.Ioc
{
    public static class ContainerBuilderExtensions
    {
        public static ContainerBuilder RegisterRequiredServices(this ContainerBuilder containerBuilder)
        {
            if (containerBuilder == null)
                throw new ArgumentNullException(nameof(containerBuilder));

            containerBuilder.RegisterType<DefaultDateTimeProvider>().As<IDateTimeProvider>().SingleInstance().PreserveExistingDefaults();

            return containerBuilder;
        }

        //public static ContainerBuilder RegisterIdentityClient(this ContainerBuilder containerBuilder)
        //{
        //    if (containerBuilder == null)
        //        throw new ArgumentNullException(nameof(containerBuilder));

        //    containerBuilder.RegisterType<DefaultSecurityService>().As<ISecurityService>().SingleInstance().PropertiesAutowired(PropertyWiringOptions.PreserveSetValues).PreserveExistingDefaults();

        //    containerBuilder.Register((c, parameters) =>
        //    {
        //        return new TokenClient(address: new Uri(c.Resolve<IClientAppProfile>().HostUri, "core/connect/token").ToString(), clientId: parameters.Named<string>("clientId"), clientSecret: parameters.Named<string>("secret"), innerHttpMessageHandler: c.ResolveNamed<HttpMessageHandler>(ContractKeys.DefaultHttpMessageHandler));
        //    }).PreserveExistingDefaults();

        //    return containerBuilder;
        //}

        //public static ContainerBuilder RegisterHttpClient<THttpMessageHandler>(this ContainerBuilder containerBuilder)
        //    where THttpMessageHandler : HttpMessageHandler, new()
        //{
        //    if (containerBuilder == null)
        //        throw new ArgumentNullException(nameof(containerBuilder));

        //    containerBuilder
        //        .RegisterType<THttpMessageHandler>()
        //        .Named<HttpMessageHandler>(ContractKeys.DefaultHttpMessageHandler)
        //        .SingleInstance()
        //        .PreserveExistingDefaults();

        //    containerBuilder.Register<HttpMessageHandler>(c =>
        //    {
        //        return new AuthenticatedHttpMessageHandler(c.Resolve<IEventAggregator>(), c.Resolve<ISecurityService>(), c.ResolveNamed<HttpMessageHandler>(ContractKeys.DefaultHttpMessageHandler));
        //    })
        //    .Named<HttpMessageHandler>(ContractKeys.AuthenticatedHttpMessageHandler)
        //    .SingleInstance()
        //    .PreserveExistingDefaults();

        //    containerBuilder.Register(c =>
        //    {
        //        HttpMessageHandler authenticatedHttpMessageHandler = c.ResolveNamed<HttpMessageHandler>(ContractKeys.AuthenticatedHttpMessageHandler);
        //        HttpClient httpClient = new HttpClient(authenticatedHttpMessageHandler)
        //        {
        //            BaseAddress = c.Resolve<IClientAppProfile>().HostUri,
        //            Timeout = TimeSpan.FromMinutes(45)
        //        };
        //        return httpClient;
        //    }).SingleInstance()
        //    .PreserveExistingDefaults();

        //    return containerBuilder;
        //}

        //public static ContainerBuilder RegisterHttpClient(this ContainerBuilder containerBuilder)
        //{
        //    if (containerBuilder == null)
        //        throw new ArgumentNullException(nameof(containerBuilder));

        //    return RegisterHttpClient<BitHttpClientHandler>(containerBuilder);
        //}

        //public static ContainerBuilder RegisterODataClient(this ContainerBuilder containerBuilder)
        //{
        //    if (containerBuilder == null)
        //        throw new ArgumentNullException(nameof(containerBuilder));

        //    Simple.OData.Client.V4Adapter.Reference();

        //    containerBuilder.Register(c =>
        //    {
        //        HttpMessageHandler authenticatedHttpMessageHandler = c.ResolveNamed<HttpMessageHandler>(ContractKeys.AuthenticatedHttpMessageHandler);

        //        IClientAppProfile clientAppProfile = c.Resolve<IClientAppProfile>();

        //        IODataClient odataClient = new ODataClient(new ODataClientSettings(new Uri(clientAppProfile.HostUri, clientAppProfile.ODataRoute))
        //        {
        //            RenewHttpConnection = false,
        //            OnCreateMessageHandler = () => authenticatedHttpMessageHandler
        //        });

        //        return odataClient;
        //    }).PreserveExistingDefaults();

        //    containerBuilder
        //        .Register(c => new ODataBatch(c.Resolve<IODataClient>(), reuseSession: true))
        //        .PreserveExistingDefaults();

        //    return containerBuilder;
        //}

        //public static ContainerBuilder RegisterDbContext<TDbContext>(this ContainerBuilder containerBuilder)
        //    where TDbContext : EfCoreDbContextBase
        //{
        //    if (containerBuilder == null)
        //        throw new ArgumentNullException(nameof(containerBuilder));

        //    containerBuilder
        //        .RegisterType<TDbContext>()
        //        .As(new[] { typeof(EfCoreDbContextBase).GetTypeInfo(), typeof(TDbContext).GetTypeInfo() })
        //        .PropertiesAutowired(PropertyWiringOptions.PreserveSetValues);

        //    return containerBuilder;
        //}

        //public static ContainerBuilder RegisterDefaultSyncService(this ContainerBuilder containerBuilder, Action<ISyncService> configureDtoSetSyncConfigs)
        //{
        //    containerBuilder.RegisterType<DefaultSyncService>().As<ISyncService>()
        //        .PropertiesAutowired(PropertyWiringOptions.PreserveSetValues)
        //        .OnActivated(config =>
        //        {
        //            configureDtoSetSyncConfigs?.Invoke(config.Instance);
        //        }).SingleInstance();

        //    return containerBuilder;
        //}
    }
}
