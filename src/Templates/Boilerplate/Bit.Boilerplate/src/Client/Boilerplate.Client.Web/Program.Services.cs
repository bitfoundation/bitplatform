//+:cnd:noEmit

using Boilerplate.Client.Web.Infrastructure.Services;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Boilerplate.Client.Core.Infrastructure.Services.HttpMessageHandlers;

namespace Boilerplate.Client.Web;

public static partial class Program
{
    extension(WebAssemblyHostBuilder builder)
    {
        public void ConfigureServices()
        {
            var services = builder.Services;
            var configuration = builder.Configuration;
            // The following services are blazor web assembly only.

            builder.Logging.ConfigureLoggers(configuration);
            //#if (sentry == true)
            builder.Logging.AddSentry(options =>
            {
                options.Debug = AppEnvironment.IsDevelopment();
                options.Environment = AppEnvironment.Current;
                configuration.Bind("Logging:Sentry", options);
            });
            //#endif

            services.AddClientWebProjectServices(configuration);

            Uri.TryCreate(configuration.GetServerAddress(), UriKind.RelativeOrAbsolute, out var serverAddress);

            if (serverAddress!.IsAbsoluteUri is false)
            {
                serverAddress = new Uri(new Uri(builder.HostEnvironment.BaseAddress), serverAddress);
            }

            services.AddScoped<HttpClient>(sp =>
            {
                var handlerFactory = sp.GetRequiredService<HttpMessageHandlersChainFactory>();
                var httpClient = new HttpClient(handlerFactory.Invoke())
                {
                    BaseAddress = serverAddress
                };

                httpClient.DefaultRequestHeaders.Add("X-Origin", builder.HostEnvironment.BaseAddress);

                return httpClient;
            });
            services.AddScoped<ClientExceptionHandlerBase, WebClientExceptionHandler>();
            services.AddScoped<SharedExceptionHandler>(sp => sp.GetRequiredService<ClientExceptionHandlerBase>());

            services.AddTransient<IPrerenderStateService, WebClientPrerenderStateService>();
        }
    }

    extension(IServiceCollection services)
    {
        public void AddClientWebProjectServices(IConfiguration configuration)
        {
            services.AddClientCoreProjectServices(configuration);
            // The following services work both in blazor web assembly and server side for pre-rendering and blazor server.

            services.AddScoped<IBitDeviceCoordinator, WebDeviceCoordinator>();
            services.AddScoped<IStorageService, WebStorageService>();
            //#if (notification == true)
            services.AddScoped<IPushNotificationService, WebPushNotificationService>();
            //#endif
            services.AddScoped<IWebAuthnService, WebAuthnService>();
            services.AddScoped<IAppUpdateService, WebAppUpdateService>();

            services.AddSingleton(sp =>
            {
                ClientWebSettings settings = new();
                configuration.Bind(settings);
                return settings;
            });

            services.AddOptions<ClientWebSettings>()
                .Bind(configuration)
                .ValidateDataAnnotations()
                .ValidateOnStart();
        }
    }
}
