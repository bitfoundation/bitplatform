using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Bit.BlazorUI;

[BitAssetTrimmingRegistrar]
public static class IBlazorUIExtrasServiceCollectionExtensions
{
    /// <summary>
    /// Registers required services of the Extras package of the BitBlazorUI components.
    /// </summary>
    /// <param name="services">The service collection to add the services to.</param>
    /// <param name="trySingleton">
    /// Tries to register the eligible services as singleton instead of scoped.
    /// Only enable this for single-user hosting models (Blazor WebAssembly and Hybrid/MAUI).
    /// Do NOT enable it on Blazor Server: services such as <see cref="BitModalService"/> hold per-circuit
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
    /// <param name="commandPalette">
    /// Configures the app-wide <see cref="BitCommandPaletteOptions"/> of <see cref="BitCommandPalette"/>: shortcuts, modes,
    /// sources, ranking weights, strings. The palette's own parameters, and a <see cref="BitCommandPaletteParams"/> cascaded
    /// through a <see cref="BitParams"/>, override what they share with it. When null, the defaults are used. In a Blazor Web
    /// App, call this from a service-registration method both the server and the client compile, so both run on the same options.
    /// </param>
    /// <returns></returns>
    public static IServiceCollection AddBitBlazorUIExtrasServices(this IServiceCollection services,
                                                                  bool trySingleton = false,
                                                                  Action<BitAccentColorConfig>? accentColor = null,
                                                                  Action<BitCommandPaletteOptions>? commandPalette = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddBitBlazorUIServices(trySingleton);

        // Nothing to do here for the packaged theme presets: this method lives in the Extras assembly,
        // so calling it is already the first touch that runs the module initializer behind
        // BitExtraThemeRegistration - the presets are in BitThemePresetRegistry before this line is
        // reached, and so before the first host page renders.

        if (accentColor is not null)
        {
            var accentColorConfig = new BitAccentColorConfig();
            accentColor(accentColorConfig);
            services.TryAddSingleton(accentColorConfig);
        }

        if (trySingleton)
        {
            services.TryAddSingleton<BitMessageBoxService>();
        }
        else
        {
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

        TryAddCommandPaletteServices(services, commandPalette);

        return services;
    }

    // The command palette's registry, open state and history are scoped regardless of trySingleton: the history store
    // takes the scoped IJSRuntime, and the open state belongs to one circuit on Blazor Server. Scoped is one per
    // application on WebAssembly and Hybrid anyway. An app that lazy loads Bit.BlazorUI.Extras on WebAssembly cannot
    // call AddBitBlazorUIExtrasServices; BitCommandPaletteRuntime then creates these services itself.
    private static void TryAddCommandPaletteServices(IServiceCollection services, Action<BitCommandPaletteOptions>? configure)
    {
        var options = new BitCommandPaletteOptions();
        configure?.Invoke(options);

        services.TryAddSingleton(options);
        services.TryAddSingleton(sp => new BitCommandPaletteRanker(sp.GetRequiredService<BitCommandPaletteOptions>()));
        services.TryAddScoped<BitCommandPaletteService>();
        services.TryAddScoped<IBitCommandPaletteService>(sp => sp.GetRequiredService<BitCommandPaletteService>());
        services.TryAddScoped<IBitCommandPaletteHistoryStore, BitCommandPaletteBrowserStorageHistoryStore>();
        services.TryAddScoped<BitCommandPaletteHistory>();
        services.TryAddSingleton(TimeProvider.System);
    }
}
