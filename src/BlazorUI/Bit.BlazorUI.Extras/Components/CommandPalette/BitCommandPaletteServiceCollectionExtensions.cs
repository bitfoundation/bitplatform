using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Bit.BlazorUI;

/// <summary>
/// Extension methods to register the command palette's services in the DI container.
/// </summary>
[BitAssetTrimmingRegistrar]
public static class BitCommandPaletteServiceCollectionExtensions
{
    /// <summary>
    /// Registers everything the palette needs. Add <c>&lt;BitCommandPalette /&gt;</c> once to a layout and inject
    /// <see cref="IBitCommandPaletteService"/> wherever commands are registered. The options are a singleton; the
    /// registry, the open state and the history are scoped, which is per circuit on Blazor Server and per
    /// application on WebAssembly. In a Blazor Web App, call it in both the server and the client project:
    /// <code>builder.Services.AddBitCommandPaletteServices();</code>
    /// A WebAssembly app that lazy loads Bit.BlazorUI.Extras leaves it out: the components then create these services
    /// themselves, with the default options, and log one warning saying so; <see cref="GetBitCommandPaletteService"/> hands out
    /// the same <see cref="IBitCommandPaletteService"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional global configuration: shortcuts, modes, sources, ranking weights, strings.</param>
    public static IServiceCollection AddBitCommandPaletteServices(this IServiceCollection services, Action<BitCommandPaletteOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new BitCommandPaletteOptions();
        configure?.Invoke(options);

        services.TryAddSingleton(options);
        services.TryAddSingleton(sp => new BitCommandPaletteRanker(sp.GetRequiredService<BitCommandPaletteOptions>()));
        services.TryAddScoped<BitCommandPaletteService>();
        services.TryAddScoped<IBitCommandPaletteService>(sp => sp.GetRequiredService<BitCommandPaletteService>());
        services.TryAddScoped<IBitCommandPaletteHistoryStore, BitCommandPaletteBrowserStorageHistoryStore>();
        services.TryAddScoped<BitCommandPaletteHistory>();
        services.TryAddSingleton(TimeProvider.System);
        return services;
    }

    /// <summary>
    /// Replaces where recents, pins and frecency are kept - by default the browser's <c>localStorage</c> -
    /// for example with per-user storage on the server.
    /// </summary>
    public static IServiceCollection AddBitCommandPaletteHistoryStore<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TStore>(this IServiceCollection services)
        where TStore : class, IBitCommandPaletteHistoryStore
    {
        ArgumentNullException.ThrowIfNull(services);

        services.Replace(ServiceDescriptor.Scoped<IBitCommandPaletteHistoryStore, TStore>());
        return services;
    }

    /// <summary>Keeps the history in memory only, for the lifetime of the scope, so nothing is written to the browser.</summary>
    public static IServiceCollection AddBitCommandPaletteInMemoryHistoryStore(this IServiceCollection services)
        => services.AddBitCommandPaletteHistoryStore<BitCommandPaletteInMemoryHistoryStore>();

    /// <summary>Registers an <see cref="IBitCommandPaletteSource"/> that is resolved from DI and queried in the default mode.</summary>
    public static IServiceCollection AddBitCommandPaletteSource<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TSource>(this IServiceCollection services)
        where TSource : class, IBitCommandPaletteSource
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IBitCommandPaletteSource, TSource>();
        return services;
    }

    /// <summary>
    /// Registers an <see cref="IBitCommandPaletteCommandGuard"/>, resolved from the scope and asked before every command runs - the
    /// place for permission checks that hold for many commands. Registering the same guard twice registers it once.
    /// </summary>
    public static IServiceCollection AddBitCommandPaletteCommandGuard<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TGuard>(this IServiceCollection services)
        where TGuard : class, IBitCommandPaletteCommandGuard
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddEnumerable(ServiceDescriptor.Scoped<IBitCommandPaletteCommandGuard, TGuard>());
        return services;
    }

    /// <summary>
    /// The <see cref="IBitCommandPaletteService"/> the palettes of this circuit or WebAssembly runtime use: the one
    /// <see cref="AddBitCommandPaletteServices"/> registered, otherwise the one BitCommandPalette creates itself. Inject
    /// <see cref="IBitCommandPaletteService"/> instead wherever the services are registered; this is for an app that lazy loads
    /// Bit.BlazorUI.Extras on WebAssembly and so cannot register them:
    /// <code>
    /// @inject IServiceProvider Services
    ///
    /// var palette = Services.GetBitCommandPaletteService();
    /// </code>
    /// </summary>
    /// <param name="services">The services of the component asking, as injected through <see cref="IServiceProvider"/>.</param>
    public static IBitCommandPaletteService GetBitCommandPaletteService(this IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // A registered IBitCommandPaletteService wins even without a BitCommandPaletteService behind it - a test double, or a decorator.
        return services.GetService<IBitCommandPaletteService>() ?? BitCommandPaletteRuntime.Of(services).Service;
    }
}
