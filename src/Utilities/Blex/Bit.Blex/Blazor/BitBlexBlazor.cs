using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Bit.Blex.Blazor;

/// <summary>Blazor-specific DI helpers for wiring Blex persistence to browser storage.</summary>
public static class BitBlexBlazor
{
    /// <summary>
    /// Persists stores marked <c>[BlexStoreAttribute(Persist = true)]</c> to <c>window.localStorage</c>, surviving
    /// full page reloads. Call after <c>AddBitBlexServices</c> and the store registrations.
    /// </summary>
    public static IServiceCollection AddBitBlexLocalStoragePersistence(
        this IServiceCollection services,
        Action<BlexPersistenceOptions>? configure = null)
        => services.AddBlexBrowserPersistence(BlexBrowserStorageKind.Local, configure);

    /// <summary>
    /// Persists stores marked <c>[BlexStoreAttribute(Persist = true)]</c> to <c>window.sessionStorage</c>, cleared
    /// when the tab closes. Call after <c>AddBitBlexServices</c> and the store registrations.
    /// </summary>
    public static IServiceCollection AddBitBlexSessionStoragePersistence(
        this IServiceCollection services,
        Action<BlexPersistenceOptions>? configure = null)
        => services.AddBlexBrowserPersistence(BlexBrowserStorageKind.Session, configure);

    private static IServiceCollection AddBlexBrowserPersistence(
        this IServiceCollection services,
        BlexBrowserStorageKind kind,
        Action<BlexPersistenceOptions>? configure)
    {
        services.AddScoped<IBlexStorage>(sp => new BlexBrowserStorage(
            sp.GetRequiredService<IJSRuntime>(), kind));
        services.AddBitBlexPersistence(configure);
        return services;
    }
}
