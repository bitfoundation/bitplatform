using Bit.ViewModel.Contracts;
using Simple.OData.Client;
using System;
using System.Net.Http;

namespace Autofac
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
                IClientAppProfile clientAppProfile = c.Resolve<IClientAppProfile>();

                IODataClient odataClient = new ODataClient(new ODataClientSettings(httpClient: c.Resolve<HttpClient>(), new Uri(clientAppProfile.ODataRoute, uriKind: UriKind.Relative))
                {
                    RenewHttpConnection = false,
                    NameMatchResolver = ODataNameMatchResolver.AlpahumericCaseInsensitive
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
