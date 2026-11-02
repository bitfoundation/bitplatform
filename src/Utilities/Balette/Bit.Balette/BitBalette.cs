using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Bit.Balette;

/// <summary>
/// Extension methods to register the Bit.Balette services in the DI container.
/// </summary>
public static class BitBalette
{
    /// <summary>
    /// Registers everything the palette needs. Add <c>&lt;Balette /&gt;</c> once to a layout and inject
    /// <see cref="IBaletteService"/> wherever commands are registered. The options are a singleton; the
    /// registry, the open state and the history are scoped, which is per circuit on Blazor Server and per
    /// application on WebAssembly. In a Blazor Web App, call it in both the server and the client project:
    /// <code>builder.Services.AddBitBaletteServices();</code>
    /// A WebAssembly app that lazy loads Bit.Balette leaves it out: the components then create these services
    /// themselves, with the default options, and log one warning saying so; <see cref="GetBaletteService"/> hands out
    /// the same <see cref="IBaletteService"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional global configuration: shortcuts, modes, sources, ranking weights, strings.</param>
    public static IServiceCollection AddBitBaletteServices(this IServiceCollection services, Action<BaletteOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new BaletteOptions();
        configure?.Invoke(options);

        services.TryAddSingleton(options);
        services.TryAddSingleton(sp => new BaletteRanker(sp.GetRequiredService<BaletteOptions>()));
        services.TryAddScoped<BaletteService>();
        services.TryAddScoped<IBaletteService>(sp => sp.GetRequiredService<BaletteService>());
        services.TryAddScoped<IBaletteHistoryStore, BaletteBrowserStorageHistoryStore>();
        services.TryAddScoped<BaletteHistory>();
        services.TryAddSingleton(TimeProvider.System);
        return services;
    }

    /// <summary>
    /// Replaces where recents, pins and frecency are kept - by default the browser's <c>localStorage</c> -
    /// for example with per-user storage on the server.
    /// </summary>
    public static IServiceCollection AddBitBaletteHistoryStore<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TStore>(this IServiceCollection services)
        where TStore : class, IBaletteHistoryStore
    {
        ArgumentNullException.ThrowIfNull(services);

        services.Replace(ServiceDescriptor.Scoped<IBaletteHistoryStore, TStore>());
        return services;
    }

    /// <summary>Keeps the history in memory only, for the lifetime of the scope, so nothing is written to the browser.</summary>
    public static IServiceCollection AddBitBaletteInMemoryHistoryStore(this IServiceCollection services)
        => services.AddBitBaletteHistoryStore<BaletteInMemoryHistoryStore>();

    /// <summary>Registers an <see cref="IBaletteSource"/> that is resolved from DI and queried in the default mode.</summary>
    public static IServiceCollection AddBitBaletteSource<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TSource>(this IServiceCollection services)
        where TSource : class, IBaletteSource
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IBaletteSource, TSource>();
        return services;
    }

    /// <summary>
    /// The <see cref="IBaletteService"/> the palettes of this circuit or WebAssembly runtime use: the one
    /// <see cref="AddBitBaletteServices"/> registered, otherwise the one Balette creates itself. Inject
    /// <see cref="IBaletteService"/> instead wherever the services are registered; this is for an app that lazy loads
    /// Bit.Balette on WebAssembly and so cannot register them:
    /// <code>
    /// @inject IServiceProvider Services
    ///
    /// var palette = Services.GetBaletteService();
    /// </code>
    /// </summary>
    /// <param name="services">The services of the component asking, as injected through <see cref="IServiceProvider"/>.</param>
    public static IBaletteService GetBaletteService(this IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // A registered IBaletteService wins even without a BaletteService behind it - a test double, or a decorator.
        return services.GetService<IBaletteService>() ?? BaletteRuntime.Of(services).Service;
    }
}
