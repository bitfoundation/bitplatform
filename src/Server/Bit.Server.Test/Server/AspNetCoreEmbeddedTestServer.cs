using Autofac.Core;
using Autofac.Core.Activators.Delegate;
using Autofac.Core.Activators.ProvidedInstance;
using Autofac.Core.Registration;
using Autofac.Extensions.DependencyInjection;
using Bit.Owin;
using Bit.Test.Implementations;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenQA.Selenium;
using OpenQA.Selenium.Remote;
using System;
using System.Linq;
using System.Net.Http;
using System.Reflection;

namespace Bit.Test.Server
{
    public class AspNetCoreEmbeddedTestServer : TestServerBase
    {
        private TestServer? _server;
        private TestEnvironmentArgs _args;

        public AspNetCoreEmbeddedTestServer(TestEnvironmentArgs args)
        {
            _args = args;
        }

        public override void Initialize(string uri)
        {
            base.Initialize(uri);

            IHostBuilder hostBuilder = BitWebHost.CreateWebHost(Array.Empty<string>())
                .ConfigureWebHost(webHostBuilder =>
                {
                    webHostBuilder.UseUrls(uri);
                    webHostBuilder.UseTestServer();
                });

            _server = hostBuilder.Start().GetTestServer();

            _server.Features.Set<IServerAddressesFeature>(new TestServerAddressesFeature(uri));
        }

        protected override void Dispose(bool disposing)
        {
            _server?.Services.GetRequiredService<IHostApplicationLifetime>().StopApplication();

            // workaround https://github.com/autofac/Autofac.Extensions.DependencyInjection/issues/87

            if (_server?.Services is AutofacServiceProvider autofacServiceProvider)
            {
                FieldInfo _lateBuildPipeline = typeof(ComponentRegistration).GetField("_lateBuildPipeline", BindingFlags.NonPublic | BindingFlags.Instance);

                FieldInfo _builtComponentPipeline = typeof(ComponentRegistration).GetField("_builtComponentPipeline", BindingFlags.NonPublic | BindingFlags.Instance);

                FieldInfo serviesField = typeof(ComponentRegistration).GetField("<Services>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);

                FieldInfo activatorField = typeof(ComponentRegistration).GetField("<Activator>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);

                foreach (IComponentRegistration regComponent in autofacServiceProvider.LifetimeScope.ComponentRegistry.Registrations)
                {
                    _lateBuildPipeline.SetValue(regComponent, null);
                    _builtComponentPipeline.SetValue(regComponent, null);
                    serviesField.SetValue(regComponent, null);
                    activatorField.SetValue(regComponent, null);
                }

                ProvidedInstanceActivator[] providedInstanceActivators = autofacServiceProvider.LifetimeScope.ComponentRegistry.Registrations.Select(r => r.Activator).OfType<ProvidedInstanceActivator>().ToArray();

                FieldInfo instanceField = typeof(ProvidedInstanceActivator).GetField("_instance", BindingFlags.NonPublic | BindingFlags.Instance);

                foreach (ProvidedInstanceActivator activator in providedInstanceActivators)
                {
                    instanceField.SetValue(activator, null);
                }

                DelegateActivator[] delegateActivators = autofacServiceProvider.LifetimeScope.ComponentRegistry.Registrations.Select(r => r.Activator).OfType<DelegateActivator>().ToArray();

                FieldInfo activationFunction = typeof(DelegateActivator).GetField("_activationFunction", BindingFlags.NonPublic | BindingFlags.Instance);

                foreach (DelegateActivator activator in delegateActivators)
                {
                    activationFunction.SetValue(activator, null);
                }

                autofacServiceProvider.LifetimeScope.ComponentRegistry.GetType().GetField("_registeredServicesTracker", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(autofacServiceProvider.LifetimeScope.ComponentRegistry, null);

                typeof(AutofacServiceProvider).GetField("_lifetimeScope", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(autofacServiceProvider, null);

                typeof(TestServer).GetField("<Services>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(_server, null);
            }

            _server?.Dispose();
        }

        protected override HttpMessageHandler GetHttpMessageHandler()
        {
            return new TestHttpClientHandler(_server!.CreateHandler(), _args);
        }

        public override WebDriver BuildWebDriver(WebDriverOptions? options = null)
        {
            throw new NotSupportedException();
        }
    }
}
