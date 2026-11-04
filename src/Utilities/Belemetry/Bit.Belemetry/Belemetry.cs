using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Bit.Belemetry;

/// <summary>Registration of Belemetry.</summary>
public static class Belemetry
{
    /// <summary>
    /// Registers Belemetry: the <c>ILogger</c> provider, the <c>Activity</c> and <c>Meter</c> collectors, the exporters,
    /// <see cref="IBelemetry"/> and <see cref="IBelemetryConsent"/>.
    /// </summary>
    /// <remarks>
    /// Call it in every project that hosts Blazor: the client (WebAssembly), the server, or the MAUI/WPF/WinForms app.
    /// It is idempotent: services are added with <c>TryAdd</c>, and every <paramref name="configure"/> callback passed
    /// across calls runs, in order, on the same <see cref="BelemetryOptions"/> (standard Options pattern).
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    public static IServiceCollection AddBitBelemetryServices(this IServiceCollection services, Action<BelemetryOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var optionsBuilder = services.AddOptions<BelemetryOptions>();
        if (configure is not null) optionsBuilder.Configure(configure);

        // Decided here, from what is already registered: WebApplication.CreateBuilder registers IWebHostEnvironment
        // before user code runs, and Belemetry cannot reference ASP.NET Core hosting (it also runs in the browser).
        services.TryAddSingleton(new BelemetryHostDetection(
            services.Any(d => d.ServiceType.FullName == "Microsoft.AspNetCore.Hosting.IWebHostEnvironment")));

        services.TryAddSingleton<BelemetrySettings>();
        services.TryAddSingleton(sp =>
        {
            var settings = sp.GetRequiredService<BelemetrySettings>();
            var saved = BelemetryConsentFile.Load(settings);

            // Narrowed to the configured features, as IBelemetryConsent.Granted and the script's grant are.
            return saved is null
                ? new BelemetryConsentState(settings.Options.Consent.Default & settings.ConfiguredFeatures, decided: false)
                : new BelemetryConsentState((BelemetryFeatures)saved.Features & settings.ConfiguredFeatures, decided: true);
        });
        services.TryAddSingleton<BelemetryBrowserBridgeRegistry>();
        services.TryAddSingleton<BelemetryPipeline>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, BelemetryLoggerProvider>());

        services.TryAddScoped<BelemetryJsBridge>();
        services.TryAddScoped<IBelemetryConsent, BelemetryConsentService>();
        services.TryAddScoped<IBelemetry, BelemetryService>();
        services.TryAddScoped<IBelemetryDiagnostics, BelemetryDiagnosticsService>();

        // The framework registers its component ActivitySource for server-side rendering only. Registered here (public,
        // idempotent) so the Navigate and HandleEvent activities - the route templates and component names diagnostics
        // and traces rest on - exist in WebAssembly and Hybrid too. Nothing is created until something listens.
        ComponentsMetricsServiceCollectionExtensions.AddComponentsTracing(services);

        return services;
    }

    /// <summary>
    /// Registers an <see cref="IBelemetrySnapshotProvider"/> (scoped, so it can read the state of the user's scope - a
    /// circuit on Blazor Server). Its snapshots are only taken when a capture asks for them.
    /// </summary>
    public static IServiceCollection AddBitBelemetrySnapshotProvider<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TProvider>(this IServiceCollection services)
        where TProvider : class, IBelemetrySnapshotProvider
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IBelemetrySnapshotProvider, TProvider>());
        return services;
    }
}
