using Bit.Butil;
using Bit.Belemetry;
using Microsoft.AspNetCore.Components.WebAssembly.Services;

namespace Microsoft.Extensions.DependencyInjection;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddLandingPageClientServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddLocalization();

        services.AddBitBlazorUIServices();
        services.AddBitBlazorUIExtrasServices();
        services.AddBitButilServices();

        services.TryAddScoped<LazyAssemblyLoader>();
        services.TryAddScoped<LandingConsentPrompt>();
        services.TryAddScoped<ILandingChatClient, WebSocketLandingChatClient>();
        services.TryAddScoped<IWaitlistClient, HttpWaitlistClient>();
        services.TryAddScoped<IStoreListingClient, HttpStoreListingClient>();

        services.AddBitBelemetryServices(options =>
        {
            options.ServiceName = "Boilerplate.LandingPage";

            if (string.IsNullOrWhiteSpace(configuration["Belemetry:AzureMonitorConnectionString"]) is false)
            {
                options.AddAzureMonitorExporter(configuration["Belemetry:AzureMonitorConnectionString"]!);
            }
        });

        return services;
    }

    public static IConfigurationBuilder AddLandingPageClientConfigurations(this IConfigurationBuilder builder, string environment)
    {
        var assembly = typeof(IServiceCollectionExtensions).Assembly;

        foreach (var name in new[] { "appsettings.json", $"appsettings.{environment}.json" })
        {
            if (assembly.GetManifestResourceStream($"Boilerplate.LandingPage.Client.{name}") is { } stream)
            {
                builder.AddJsonStream(stream);
            }
        }

        return builder;
    }
}
