using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Bit.BlazorUI;

public static class IBlazorUIExtrasServiceCollectionExtensions
{
    /// <summary>
    /// Registers required services of the Extras package of the BitBlazorUI components.
    /// </summary>
    /// <param name="services">The service collection to add the services to.</param>
    /// <param name="trySingleton">
    /// Tries to register the eligible services as singleton instead of scoped.
    /// Only enable this for single-user hosting models (Blazor WebAssembly and Hybrid/MAUI).
    /// Do NOT enable it on Blazor Server: services such as <see cref="BitProModalService"/> hold per-circuit
    /// rendering state (the active modal container), and a singleton would be shared across circuits,
    /// leaking modals between users.
    /// </param>
    /// <param name="accentColor">
    /// Configures the app-wide <see cref="BitAccentColorConfig"/> and registers it in DI, where
    /// <see cref="BitAccentColorHead"/>, <see cref="BitAccentColorSwitcher"/> and
    /// <see cref="BitAccentColorService"/> fall back to it whenever no explicit Config is handed to
    /// them. Call this from a service-registration method both the server and the client compile
    /// (the usual shared AddClientServices-style extension), so the configuration is stated once
    /// and every container - the one that renders the host page's head and the one the switchers
    /// run in - resolves the same values.
    /// </param>
    /// <returns></returns>
    public static IServiceCollection AddBitBlazorUIExtrasServices(this IServiceCollection services, bool trySingleton = false, Action<BitAccentColorConfig>? accentColor = null)
    {
        services.AddBitBlazorUIServices(trySingleton);

        if (accentColor is not null)
        {
            var accentColorConfig = new BitAccentColorConfig();
            accentColor(accentColorConfig);
            services.TryAddSingleton(accentColorConfig);
        }

        if (trySingleton)
        {
            services.TryAddSingleton<BitProModalService>();
            services.TryAddSingleton<BitMessageBoxService>();
        }
        else
        {
            services.TryAddScoped<BitProModalService>();
            services.TryAddScoped<BitMessageBoxService>();
        }

        // Never singleton, regardless of trySingleton: its constructor takes IJSRuntime,
        // BitThemeManager and BitThemeNotifications, which AddBitBlazorUIServices registers as
        // scoped. A singleton would capture the root scope's instances - in Blazor Hybrid a
        // JS runtime that is never attached to any WebView - and either break interop silently or
        // throw under scope validation. Scoped is also the lifetime the accent needs: one shared
        // color per circuit/WebView, alive across every switcher instance.
        services.TryAddScoped<BitAccentColorService>();

        services.TryAddScoped<BitExtraServices>();

        return services;
    }
}
