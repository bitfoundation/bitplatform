using Bit.ViewModel.Contracts;
using Bit.ViewModel.Implementations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Logging;
using Prism.Events;
using Refit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;

namespace Autofac
{
    public static class ContainerBuilderExtensions
    {
        public static ContainerBuilder RegisterRefitClient(this ContainerBuilder containerBuilder)
        {
            if (containerBuilder == null)
                throw new ArgumentNullException(nameof(containerBuilder));

            containerBuilder.RegisterType<BitRefitJsonContentSerializer>() // This needs to be registered once, but using current approach it will be registered multiple times, but this is fine!
                .As<IContentSerializer>()
                .SingleInstance()
                .PropertiesAutowired()
                .PreserveExistingDefaults();

            containerBuilder.Register(context => new RefitSettings
            {
                ContentSerializer = context.Resolve<IContentSerializer>()
            }).PreserveExistingDefaults();

            return containerBuilder;
        }

        public static ContainerBuilder RegisterRefitService<TService>(this ContainerBuilder containerBuilder)
            where TService : notnull
        {
            if (containerBuilder == null)
                throw new ArgumentNullException(nameof(containerBuilder));

            containerBuilder.Register(c => RestService.For<TService>(c.Resolve<HttpClient>(), c.Resolve<RefitSettings>())).PreserveExistingDefaults();

            return containerBuilder;
        }

        public static ContainerBuilder RegisterIdentityClient(this ContainerBuilder containerBuilder)
        {
            return containerBuilder.RegisterIdentityClient<DefaultSecurityService>();
        }

        public static ContainerBuilder RegisterIdentityClient<TSecurityService>(this ContainerBuilder containerBuilder)
            where TSecurityService : ISecurityService
        {
            IdentityModelEventSource.ShowPII = true;

            if (containerBuilder == null)
                throw new ArgumentNullException(nameof(containerBuilder));

            containerBuilder.RegisterType<TSecurityService>().As<ISecurityService>().SingleInstance().PropertiesAutowired(PropertyWiringOptions.PreserveSetValues).PreserveExistingDefaults();

            containerBuilder.RegisterBuildCallback(async scope =>
            {
                try
                {
                    ISecurityService securityService = scope.Resolve<ISecurityService>();
                    ITelemetryService allTelemetryServices = scope.Resolve<IEnumerable<ITelemetryService>>().All();

                    if (await securityService.IsLoggedInAsync().ConfigureAwait(false))
                    {
                        allTelemetryServices.SetUserId((await securityService.GetBitJwtToken(default).ConfigureAwait(false)).UserId!);
                    }
                    else
                    {
                        allTelemetryServices.SetUserId(null);
                    }
                }
                catch (Exception exp)
                {
                    scope.Resolve<IExceptionHandler>().OnExceptionReceived(exp);
                }
            });

            return containerBuilder;
        }

        public static IHttpClientBuilder RegisterHttpClient(this ContainerBuilder containerBuilder)
        {
            if (containerBuilder == null)
                throw new ArgumentNullException(nameof(containerBuilder));

            return RegisterHttpClient<BitHttpClientHandler>(containerBuilder);
        }

        public static IHttpClientBuilder RegisterHttpClient<THttpMessageHandler>(this ContainerBuilder containerBuilder)
            where THttpMessageHandler : HttpMessageHandler, new()
        {
            containerBuilder.RegisterHttpMessageHandler<THttpMessageHandler>();

            IServiceCollection services = (IServiceCollection)containerBuilder.Properties[nameof(services)]!;

            containerBuilder.Register(c => c.Resolve<IHttpClientFactory>().CreateClient(ContractKeys.DefaultHttpClientName))
                .PreserveExistingDefaults();

            containerBuilder.Register(context => new IPollyHttpResponseMessagePolicyFactory(request => DefaultRestFactories.BuildHttpPollyPolicy(request))).PreserveExistingDefaults();

            return services.AddHttpClient(ContractKeys.DefaultHttpClientName)
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
        }

        public static void RegisterHttpMessageHandler<THttpMessageHandler>(this ContainerBuilder containerBuilder)
            where THttpMessageHandler : HttpMessageHandler, new()
        {
            if (containerBuilder == null)
                throw new ArgumentNullException(nameof(containerBuilder));

            containerBuilder
                .RegisterType<THttpMessageHandler>()
                .Named<HttpMessageHandler>(ContractKeys.DefaultHttpMessageHandler)
                .PropertiesAutowired(PropertyWiringOptions.PreserveSetValues)
                .SingleInstance()
                .PreserveExistingDefaults();

            containerBuilder.Register<HttpMessageHandler>(c =>
            {
                return new AuthenticatedHttpMessageHandler(c.Resolve<IEventAggregator>(), c.Resolve<ISecurityService>(), c.ResolveNamed<HttpMessageHandler>(ContractKeys.DefaultHttpMessageHandler));
            })
            .Named<HttpMessageHandler>(ContractKeys.AuthenticatedHttpMessageHandler)
            .SingleInstance()
            .PreserveExistingDefaults();
        }
    }
}
