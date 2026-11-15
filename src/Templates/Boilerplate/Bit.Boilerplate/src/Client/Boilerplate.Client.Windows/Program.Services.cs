//+:cnd:noEmit
// [mirror] blazor hybrid DI registrations and logging setup - keep in sync with:
// - src/Client/Boilerplate.Client.Maui/MauiProgram.Services.cs

using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Boilerplate.Client.Windows.Infrastructure.Services;
using Boilerplate.Client.Core.Infrastructure.Services.HttpMessageHandlers;

namespace Boilerplate.Client.Windows;

public static partial class Program
{
    extension(IServiceCollection services)
    {
        public void AddClientWindowsProjectServices(IConfiguration configuration)
        {
            // Services being registered here can get injected in windows project only.
            services.AddClientCoreProjectServices(configuration);

            services.AddScoped<IWebAuthnService, WindowsWebAuthnService>();
            services.AddScoped<IExternalNavigationService, WindowsExternalNavigationService>();
            services.AddScoped<ClientExceptionHandlerBase, WindowsExceptionHandler>();
            services.AddScoped<SharedExceptionHandler>(sp => sp.GetRequiredService<ClientExceptionHandlerBase>());

            services.AddScoped<IAppUpdateService, WindowsAppUpdateService>();
            services.AddScoped<IPermissionService, WindowsPermissionService>();
            services.AddScoped<IBitDeviceCoordinator, WindowsDeviceCoordinator>();

            services.AddScoped<HttpClient>(sp =>
            {
                var handlerFactory = sp.GetRequiredService<HttpMessageHandlersChainFactory>();
                var httpClient = new HttpClient(handlerFactory.Invoke())
                {
                    BaseAddress = new Uri(configuration.GetServerAddress(), UriKind.Absolute)
                };
                var origin = sp.GetRequiredService<ClientWindowsSettings>().WebAppUrl ?? httpClient.BaseAddress;
                httpClient.DefaultRequestHeaders.Add("X-Origin", origin.ToString());
                return httpClient;
            });

            services.AddSingleton(sp => configuration);
            services.AddSingleton<IStorageService, WindowsStorageService>();
            services.AddSingleton<ILocalHttpServer, WindowsLocalHttpServer>();

            services.AddSingleton(sp => sp.GetRequiredService<IOptions<ClientWindowsSettings>>().Value);
            services.AddSingleton(ITelemetryContext.Current!);
            //#if (notification == true)
            services.AddSingleton<IPushNotificationService, WindowsPushNotificationService>();
            //#endif

            services.AddWindowsFormsBlazorWebView();
            services.AddBlazorWebViewDeveloperTools();

            services.AddLogging(loggingBuilder =>
            {
                loggingBuilder.ConfigureLoggers(configuration);
                loggingBuilder.AddEventSourceLogger();
                loggingBuilder.AddEventLog(options => configuration.Bind("Logging:EventLog", options));
            });

            services.AddSingleton<IValidateOptions<ClientWindowsSettings>, ClientWindowsSettingsValidator>();
            services.AddOptions<ClientWindowsSettings>()
                .Bind(configuration)
                .ValidateOnStart();
        }
    }
}
