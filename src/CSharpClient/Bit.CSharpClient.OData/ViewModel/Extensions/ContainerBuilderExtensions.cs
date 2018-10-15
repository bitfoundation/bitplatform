using Autofac;
using Bit.ViewModel.Contracts;
using Simple.OData.Client;
using System;
using System.Net.Http;

namespace Prism.Ioc
{
    public static class ContainerBuilderExtensions
    {
        public static ContainerBuilder RegisterODataClient(this ContainerBuilder containerBuilder)
        {
            if (containerBuilder == null)
                throw new ArgumentNullException(nameof(containerBuilder));

            Simple.OData.Client.V4Adapter.Reference();

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

            return containerBuilder;
        }
    }
}
