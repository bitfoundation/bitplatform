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
}
