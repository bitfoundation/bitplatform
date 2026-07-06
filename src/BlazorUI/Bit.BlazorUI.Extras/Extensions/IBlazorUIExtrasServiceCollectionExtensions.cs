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
    /// <returns></returns>
    public static IServiceCollection AddBitBlazorUIExtrasServices(this IServiceCollection services, bool trySingleton = false)
    {
        services.AddBitBlazorUIServices(trySingleton);

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

        services.TryAddScoped<BitExtraServices>();

        return services;
    }
}
