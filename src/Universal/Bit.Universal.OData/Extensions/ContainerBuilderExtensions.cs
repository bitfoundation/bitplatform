using Bit.ViewModel.Contracts;
using Simple.OData.Client;
using System;
using System.Net.Http;

namespace Autofac
{
    public static class ContainerBuilderExtensions
    {
        public static ContainerBuilder RegisterODataClient(this ContainerBuilder containerBuilder, Action<ODataClientSettings>? odataClientSettingsCustomizer = null)
        {
            if (containerBuilder == null)
                throw new ArgumentNullException(nameof(containerBuilder));

            Simple.OData.Client.V4Adapter.Reference();

            containerBuilder.Register(c =>
            {
                IClientAppProfile clientAppProfile = c.Resolve<IClientAppProfile>();

                ODataClientSettings settings = new ODataClientSettings(httpClient: c.Resolve<HttpClient>(), new Uri(clientAppProfile.ODataRoute ?? throw new InvalidOperationException($"{nameof(IClientAppProfile.ODataRoute)} is null."), uriKind: UriKind.RelativeOrAbsolute))
                {
                    RenewHttpConnection = false,
                    NameMatchResolver = ODataNameMatchResolver.AlpahumericCaseInsensitive
                };

                odataClientSettingsCustomizer?.Invoke(settings);

                IODataClient odataClient = new ODataClient(settings);

                return odataClient;
            }).PreserveExistingDefaults();

            containerBuilder
                .Register(c => new ODataBatch(c.Resolve<IODataClient>(), reuseSession: true))
                .PreserveExistingDefaults();

            return containerBuilder;
        }
    }
}
