using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Bit.BlazorUI;

[BitAssetTrimmingRegistrar]
public static class IBitBlazorUIServiceCollectionExtensions
{
    /// <summary>
    /// Registers the core Bit.BlazorUI services.
    /// </summary>
    /// <param name="services">The service collection to add the services to.</param>
    /// <param name="trySingleton">
    /// Tries to register the eligible services as singleton instead of scoped.
    /// Only enable this for single-user hosting models (Blazor WebAssembly and Hybrid/MAUI).
    /// Do NOT enable it on Blazor Server: services such as <see cref="BitModalService"/> hold per-circuit
    /// rendering state (the active modal container), and a singleton would be shared across circuits,
    /// leaking modals between users.
    /// </param>
    public static IServiceCollection AddBitBlazorUIServices(this IServiceCollection services, bool trySingleton = false)
    {
        services.TryAddScoped<BitThemeNotifications>(sp =>
            new BitThemeNotifications(sp.GetService<ILoggerFactory>())
            {
                // Lets a bare ThemeChanged subscription wire up the JS notifier on its own; resolved
                // lazily per call so construction stays cycle-free (manager -> receiver -> notifications)
                // and disposal of the scope surfaces as ObjectDisposedException, which the trigger handles.
                RegistrationTrigger = () => sp.GetRequiredService<BitThemeManager>().EnsureThemeNotificationsRegisteredAsync(),
            });

        // BitThemeJsNotifierReceiver is internal (consumers should listen on
        // BitThemeNotifications.ThemeChanged), but DI still resolves it for us.
        services.TryAddScoped<BitThemeJsNotifierReceiver>(sp =>
            new BitThemeJsNotifierReceiver(
                sp.GetRequiredService<BitThemeNotifications>(),
                sp.GetService<ILoggerFactory>()));

        services.TryAddScoped<BitThemeManager>(sp =>
            new BitThemeManager(
                sp.GetRequiredService<IJSRuntime>(),
                sp.GetRequiredService<BitThemeJsNotifierReceiver>(),
                sp.GetService<ILoggerFactory>()));

        services.TryAddScoped<BitExternalThemeLoader>();

        services.TryAddScoped<BitPageVisibility>();

        // The logger factory is optional: it is what turns "the modal never showed up" into a line naming the
        // missing container, and an app that registers none still gets a working service.
        if (trySingleton)
        {
            services.TryAddSingleton(sp => new BitModalService(sp.GetService<ILoggerFactory>()));
        }
        else
        {
            services.TryAddScoped(sp => new BitModalService(sp.GetService<ILoggerFactory>()));
        }

        // Scoped for the same reason as the modal service: the host it shows through belongs to one circuit.
        if (trySingleton)
        {
            services.TryAddSingleton(sp => new BitSnackBarService(sp.GetService<ILoggerFactory>()));
        }
        else
        {
            services.TryAddScoped(sp => new BitSnackBarService(sp.GetService<ILoggerFactory>()));
        }

        return services;
    }

    /// <summary>
    /// Registers the core Bit.BlazorUI services and applies the library's runtime options in the same call - the lazy
    /// scripts and where the packages' assets are served from - so an app configures the library in its Program.cs.
    /// </summary>
    /// <param name="services">The service collection to add the services to.</param>
    /// <param name="configure">Sets the options; see <see cref="BitBlazorUIOptions"/> for what each one covers.</param>
    public static IServiceCollection AddBitBlazorUIServices(this IServiceCollection services, Action<BitBlazorUIOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var options = new BitBlazorUIOptions();
        configure(options);

        switch (options.LazyScripts)
        {
            case true: BitBlazorUIScripts.UseLazyScripts(options.ContentPath); break;
            case false: BitBlazorUIScripts.UseBundledScripts(); break;
            default:
                // The mode left alone; the path still applies, for an app whose project turned the lazy scripts on and
                // serves the packages' assets from somewhere else.
                if (string.IsNullOrWhiteSpace(options.ContentPath) is false) BitBlazorUIScripts.SetContentPath(options.ContentPath);
                break;
        }

        return AddBitBlazorUIServices(services, options.TrySingleton);
    }
}
