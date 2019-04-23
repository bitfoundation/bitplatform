using Bit.ViewModel.Contracts;
using Bit.ViewModel.Implementations;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Prism.Events;
using Refit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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

            return containerBuilder;
        }

        public static ContainerBuilder RegisterRefitService<TService>(this ContainerBuilder containerBuilder)
        {
            if (containerBuilder == null)
                throw new ArgumentNullException(nameof(containerBuilder));

            containerBuilder.Register(c => RestService.For<TService>(c.Resolve<HttpClient>(), new RefitSettings
            {
                ContentSerializer = c.Resolve<IContentSerializer>()
            }));

            return containerBuilder;
        }

        public static ContainerBuilder RegisterIdentityClient(this ContainerBuilder containerBuilder)
        {
            if (containerBuilder == null)
                throw new ArgumentNullException(nameof(containerBuilder));

            containerBuilder.RegisterType<DefaultSecurityService>().As<ISecurityService>().SingleInstance().PropertiesAutowired(PropertyWiringOptions.PreserveSetValues).PreserveExistingDefaults()
                .OnActivated(activatedEventArgs =>
                {
#if iOS
                    if (Bit.iOS.BitFormsApplicationDelegate.OnSsoLoginLogoutRedirectCompleted == null)
                        Bit.iOS.BitFormsApplicationDelegate.OnSsoLoginLogoutRedirectCompleted = activatedEventArgs.Instance.OnSsoLoginLogoutRedirectCompleted;
#endif
                })
                .AutoActivate();

            return containerBuilder;
        }

        public static IHttpClientBuilder RegisterHttpClient<THttpMessageHandler>(this ContainerBuilder containerBuilder)
            where THttpMessageHandler : HttpMessageHandler, new()
        {
            containerBuilder.RegisterHttpMessageHandler<THttpMessageHandler>();

            IServiceCollection services = (IServiceCollection)containerBuilder.Properties[nameof(services)];

            containerBuilder.Register(c => c.Resolve<IHttpClientFactory>().CreateClient(ContractKeys.DefaultHttpClientName))
                .SingleInstance()
                .PreserveExistingDefaults();

            IAsyncPolicy<HttpResponseMessage> policy = containerBuilder.BuildHttpPollyPolicy();

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
                .AddPolicyHandler(policy);
        }

        public static IAsyncPolicy<HttpResponseMessage> BuildHttpPollyPolicy(this ContainerBuilder containerBuilder)
        {
            // https://github.com/App-vNext/Polly.Extensions.Http/blob/master/src/Polly.Extensions.Http/HttpPolicyExtensions.cs

            IAsyncPolicy<HttpResponseMessage> policy = Policy.Handle<HttpRequestException>() // HandleTransientHttpError
                .OrResult<HttpResponseMessage>((response) =>
                {
                    if (response.ReasonPhrase == "KnownError" || (response.Headers.TryGetValues("Reason-Phrase", out IEnumerable<string> reasonPhrases) && reasonPhrases.Any(rp => rp == "KnownError"))) // Bit Policy
                        return false;
                    return (int)response.StatusCode >= 500 || response.StatusCode == HttpStatusCode.RequestTimeout; // TransientHttpStatusCodePredicate
                })
                .WaitAndRetryAsync(new[]
                {
                    TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(5),
                    TimeSpan.FromSeconds(10)
                });

            return policy;
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

        public static IHttpClientBuilder RegisterHttpClient(this ContainerBuilder containerBuilder)
        {
            if (containerBuilder == null)
                throw new ArgumentNullException(nameof(containerBuilder));

            return RegisterHttpClient<BitHttpClientHandler>(containerBuilder);
        }
    }
}
