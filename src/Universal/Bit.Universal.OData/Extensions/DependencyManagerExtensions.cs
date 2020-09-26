using Bit.Core.Contracts;
using Bit.OData.Implementations;
using Simple.OData.Client;
using System;
using System.Net.Http;

namespace Autofac
{
    public static class DependencyManagerExtensions
    {
        public static IDependencyManager RegisterODataClient(this IDependencyManager dependencyManager, Action<IServiceProvider, ODataClientSettings>? odataClientSettingsCustomizer = null)
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            Simple.OData.Client.V4Adapter.Reference();

            dependencyManager.Register<IODataAdapterFactory, DefaultODataAdapterFactory>(lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExisting: false);

            dependencyManager.RegisterUsing(resolver =>
            {
                IClientAppProfile clientAppProfile = resolver.Resolve<IClientAppProfile>();

                ODataClientSettings settings = new ODataClientSettings(httpClient: resolver.Resolve<HttpClient>(), new Uri(clientAppProfile.ODataRoute ?? throw new InvalidOperationException($"{nameof(IClientAppProfile.ODataRoute)} is null."), uriKind: UriKind.RelativeOrAbsolute))
                {
                    AdapterFactory = resolver.Resolve<IODataAdapterFactory>(),
                    RenewHttpConnection = false,
                    NameMatchResolver = ODataNameMatchResolver.AlpahumericCaseInsensitive,
                    IncludeAnnotationsInResults = false
                };

                odataClientSettingsCustomizer?.Invoke(resolver.Resolve<IServiceProvider>(), settings);

                IODataClient odataClient = new ODataClient(settings);

                return odataClient;
            }, overwriteExisting: false, lifeCycle: DependencyLifeCycle.Transient);

            dependencyManager
                .RegisterUsing(resolver => new ODataBatch(resolver.Resolve<IODataClient>(), reuseSession: true), lifeCycle: DependencyLifeCycle.Transient, overwriteExisting: false);

            return dependencyManager;
        }
    }
}
