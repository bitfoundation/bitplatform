﻿using Bit.Core.Contracts;
using Bit.Http.Contracts;
using Bit.Http.Implementations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http;
using Microsoft.IdentityModel.Logging;
using Prism.Events;
using Refit;
using System;
using System.Net.Http;
using System.Reflection;
using System.Threading;

namespace Autofac
{
    public static class DependencyManagerExtensions
    {
        public static IDependencyManager RegisterRefitClient(this IDependencyManager dependencyManager)
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            dependencyManager.Register<IHttpContentSerializer, BitRefitJsonContentSerializer>(lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExisting: false); // This needs to be registered once, but using current approach it will be registered multiple times, but this is fine!

            dependencyManager.RegisterUsing(resolver => new RefitSettings
            {
                ContentSerializer = resolver.Resolve<IHttpContentSerializer>()
            }, overwriteExisting: false, lifeCycle: DependencyLifeCycle.Transient);

            return dependencyManager;
        }

        public static IDependencyManager RegisterRefitService<TService>(this IDependencyManager dependencyManager)
            where TService : notnull
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            dependencyManager.RegisterUsing(c => RestService.For<TService>(c.Resolve<HttpClient>(), c.Resolve<RefitSettings>()), overwriteExisting: false, lifeCycle: DependencyLifeCycle.Transient);

            return dependencyManager;
        }

        public static IDependencyManager RegisterIdentityClient(this IDependencyManager dependencyManager)
        {
            return dependencyManager.RegisterIdentityClient<DefaultSecurityService>();
        }

        public static IDependencyManager RegisterIdentityClient<TSecurityService>(this IDependencyManager dependencyManager)
            where TSecurityService : class, ISecurityService
        {
            IdentityModelEventSource.ShowPII = true;

            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            dependencyManager.Register(servicesType: new[] { typeof(ISecurityServiceBase).GetTypeInfo(), typeof(ISecurityService).GetTypeInfo() }, implementationType: typeof(TSecurityService).GetTypeInfo(), lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExisting: false);

            return dependencyManager;
        }

        public static IHttpClientBuilder RegisterHttpClient(this IDependencyManager dependencyManager)
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            return RegisterHttpClient<BitHttpClientHandler>(dependencyManager);
        }

        public static IHttpClientBuilder RegisterHttpClient<THttpMessageHandler>(this IDependencyManager dependencyManager)
            where THttpMessageHandler : HttpMessageHandler
        {
            dependencyManager.RegisterHttpMessageHandler<THttpMessageHandler>();

            IServiceCollection services = dependencyManager.GetServiceCollection();

            dependencyManager.RegisterUsing(resolver => resolver.Resolve<IHttpClientFactory>().CreateClient(Microsoft.Extensions.Options.Options.DefaultName), lifeCycle: DependencyLifeCycle.Transient, overwriteExisting: false);

            dependencyManager.RegisterUsing(resolver => new IPollyHttpResponseMessagePolicyFactory(request => DefaultRestFactories.BuildHttpPollyPolicy(request)), lifeCycle: DependencyLifeCycle.Transient, overwriteExisting: false);

            IHttpClientBuilder builder = services.AddHttpClient(Microsoft.Extensions.Options.Options.DefaultName)
                .ConfigureHttpClient((serviceProvider, httpClient) =>
                {
                    httpClient.BaseAddress = serviceProvider.GetRequiredService<IClientAppProfile>().HostUri;
                    httpClient.Timeout = Timeout.InfiniteTimeSpan;
                })
                .ConfigurePrimaryHttpMessageHandler((serviceProvider) =>
                {
                    return serviceProvider.GetRequiredService<IContainer>().ResolveNamed<HttpMessageHandler>(ContractKeys.AuthenticatedHttpMessageHandler);
                })
                .SetHandlerLifetime(Timeout.InfiniteTimeSpan)
                .AddPolicyHandler((serviceProvider, request) => serviceProvider.GetRequiredService<IPollyHttpResponseMessagePolicyFactory>()(request));

            services.RemoveAll<IHttpMessageHandlerBuilderFilter>(); // https://stackoverflow.com/a/52970073/2720104

            return builder;
        }

        public static void RegisterHttpMessageHandler<THttpMessageHandler>(this IDependencyManager dependencyManager)
            where THttpMessageHandler : HttpMessageHandler
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            ContainerBuilder containerBuilder = dependencyManager.GetContainerBuilder();

            containerBuilder.RegisterType<THttpMessageHandler>()
                .Named<HttpMessageHandler>(ContractKeys.DefaultHttpMessageHandler)
                .SingleInstance()
                .PreserveExistingDefaults();

            containerBuilder.Register((Func<IComponentContext, HttpMessageHandler>)(c =>
            {
                return new AuthenticatedHttpMessageHandler(c.Resolve<IEventAggregator>(), c.Resolve<ISecurityService>(), c.ResolveNamed<HttpMessageHandler>(ContractKeys.DefaultHttpMessageHandler));
            }))
            .Named<HttpMessageHandler>(ContractKeys.AuthenticatedHttpMessageHandler)
            .SingleInstance()
            .PreserveExistingDefaults();
        }
    }
}
