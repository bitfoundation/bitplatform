using Autofac;
using Bit.ViewModel.Contracts;
using Bit.ViewModel.Implementations;
using Prism.Events;
using System;
using System.Net.Http;

namespace Prism.Ioc
{
    public static class ContainerBuilderExtensions
    {
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

        public static ContainerBuilder RegisterHttpClient<THttpMessageHandler>(this ContainerBuilder containerBuilder)
            where THttpMessageHandler : HttpMessageHandler, new()
        {
            if (containerBuilder == null)
                throw new ArgumentNullException(nameof(containerBuilder));

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
                HttpClient httpClient = new HttpClient(authenticatedHttpMessageHandler)
                {
                    BaseAddress = c.Resolve<IClientAppProfile>().HostUri,
                    Timeout = TimeSpan.FromMinutes(45)
                };
                return httpClient;
            }).SingleInstance()
            .PreserveExistingDefaults();

            return containerBuilder;
        }

        public static ContainerBuilder RegisterHttpClient(this ContainerBuilder containerBuilder)
        {
            if (containerBuilder == null)
                throw new ArgumentNullException(nameof(containerBuilder));

            return RegisterHttpClient<BitHttpClientHandler>(containerBuilder);
        }
    }
}
