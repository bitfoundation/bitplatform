using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Bit.BlazorUI;

/// <summary>
/// Extension methods to extend the command palette's services in the DI container. The services themselves are registered by
/// <see cref="IBlazorUIExtrasServiceCollectionExtensions.AddBitBlazorUIExtrasServices"/>.
/// </summary>
[BitAssetTrimmingRegistrar]
public static class BitCommandPaletteServiceCollectionExtensions
{
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
    /// <see cref="IBlazorUIExtrasServiceCollectionExtensions.AddBitBlazorUIExtrasServices"/> registered, otherwise the one BitCommandPalette
    /// creates itself. Inject <see cref="IBitCommandPaletteService"/> instead wherever the services are registered; this is for an app that
    /// lazy loads Bit.BlazorUI.Extras on WebAssembly and so cannot register them:
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
