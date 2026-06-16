using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

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
        services.TryAddScoped<BitThemeManager>();
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
