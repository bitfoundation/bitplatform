using Bit.ViewModel.Contracts;
using Bit.ViewModel.Implementations;
using System;
using System.Net.Http;

namespace Autofac
{
    public static class ContainerBuilderExtensions
    {
        public static ContainerBuilder RegisterSignalr(this ContainerBuilder containerBuilder)
        {
            if (containerBuilder == null)
                throw new ArgumentNullException(nameof(containerBuilder));

            containerBuilder.Register(c =>
            {
                HttpMessageHandler authenticatedHttpMessageHandler = c.ResolveNamed<HttpMessageHandler>(ContractKeys.AuthenticatedHttpMessageHandler);
                SignalRHttpClient signalRHttpClient = new SignalRHttpClient(authenticatedHttpMessageHandler);
                return signalRHttpClient;
            }).SingleInstance();

            containerBuilder.RegisterType<SignalrMessageReceiver>().PropertiesAutowired(PropertyWiringOptions.PreserveSetValues)
                .As<IMessageReceiver>().SingleInstance();

            return containerBuilder;
        }
    }
}
