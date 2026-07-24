using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Bit.BlazorUI;

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

        if (trySingleton)
        {
            services.TryAddSingleton<BitModalService>();
        }
        else
        {
            services.TryAddScoped<BitModalService>();
        }

        return services;
    }
}
