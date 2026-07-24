//+:cnd:noEmit
using Microsoft.Extensions.Logging;
//#if (appInsights == true)
using Azure.Monitor.OpenTelemetry.Exporter;
//#endif
using OpenTelemetry;
using OpenTelemetry.Resources;
using System.Diagnostics.Metrics;
using Boilerplate.Client.Maui.Infrastructure.Services;
using Boilerplate.Client.Core.Infrastructure.Services.HttpMessageHandlers;

namespace Boilerplate.Client.Maui;

public static partial class MauiProgram
{
    extension(MauiAppBuilder builder)
    {
        public void ConfigureServices()
        {
            // Services being registered here can get injected in Maui (Android, iOS, macOS, Windows)
            var services = builder.Services;
            var configuration = builder.Configuration;
            services.AddClientCoreProjectServices(builder.Configuration);

            services.AddTransient<MainPage>();

            services.AddScoped<IWebAuthnService, MauiWebAuthnService>();
            services.AddScoped<ClientExceptionHandlerBase, MauiExceptionHandler>();
            services.AddScoped<SharedExceptionHandler>(sp => sp.GetRequiredService<ClientExceptionHandlerBase>());

            services.AddScoped<IAppUpdateService, MauiAppUpdateService>();
            services.AddScoped<IBitDeviceCoordinator, MauiDeviceCoordinator>();
            services.AddScoped<IExternalNavigationService, MauiExternalNavigationService>();

            services.AddScoped<HttpClient>(sp =>
            {
                var handlerFactory = sp.GetRequiredService<HttpMessageHandlersChainFactory>();
                var httpClient = new HttpClient(handlerFactory.Invoke())
                {
                    BaseAddress = new Uri(configuration.GetServerAddress(), UriKind.Absolute)
                };
                var origin = sp.GetRequiredService<ClientMauiSettings>().WebAppUrl ?? httpClient.BaseAddress;
                httpClient.DefaultRequestHeaders.Add("X-Origin", origin.ToString());
                return httpClient;
            });

            services.AddSingleton<IStorageService, MauiStorageService>();
            var settings = new ClientMauiSettings();
            configuration.Bind(settings);
            services.AddSingleton(sp =>
            {
                return settings;
            });
            services.AddSingleton(ITelemetryContext.Current!);
            services.AddSingleton<ILocalHttpServer, MauiLocalHttpServer>();

            services.AddMauiBlazorWebView();
            services.AddBlazorWebViewDeveloperTools();

            builder.Logging.ConfigureLoggers(configuration);

            builder.Logging.AddEventSourceLogger();

            builder.Logging.AddOpenTelemetry(options =>
            {
                options.IncludeScopes = true;
                options.IncludeFormattedMessage = true;
                configuration.Bind("Logging:OpenTelemetry", options);
            });

            var openTelemetry = services.AddOpenTelemetry()
                .WithMetrics(metrics =>
                {
                    metrics.AddMeter(Meter.Current.Name);
                })
                .WithTracing(tracing =>
                {
                    tracing.AddSource(ActivitySource.Current.Name);
                })
                .ConfigureResource(resource =>
                {
                    resource.AddAttributes([new("service.name", builder.Environment.ApplicationName)]);
                });

            var useOtlpExporter = string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]) is false
                || string.IsNullOrEmpty(builder.Configuration["OTEL_EXPORTER_OTLP_LOGS_ENDPOINT"]) is false;

            if (useOtlpExporter)
            {
                openTelemetry.UseOtlpExporter();
            }

            //#if (appInsights == true)
            if (string.IsNullOrEmpty(settings.ApplicationInsights?.ConnectionString) is false)
            {
                openTelemetry.UseAzureMonitorExporter(options =>
                {
                    configuration.Bind("ApplicationInsights", options);
                });
            }
            //#endif

            if (AppPlatform.IsWindows)
            {
                builder.Logging.AddEventLog(options => configuration.GetRequiredSection("Logging:EventLog").Bind(options));
            }

            services.AddOptions<ClientMauiSettings>()
                .Bind(configuration)
                .ValidateDataAnnotations()
                .ValidateOnStart();

            //-:cnd:noEmit
#if Android
            services.AddClientMauiProjectAndroidServices(builder.Configuration);
#elif iOS
        services.AddClientMauiProjectIosServices(builder.Configuration);
#elif Mac
            services.AddClientMauiProjectMacCatalystServices(builder.Configuration);
#elif Windows
        services.AddClientMauiProjectWindowsServices(builder.Configuration);
#endif
            //+:cnd:noEmit
        }
    }
}
