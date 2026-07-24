using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Bit.BlazorUI.Legacy;

public static class IBlazorUILegacyServiceCollectionExtensions
{
    /// <summary>
    /// Registers required services of the Legacy package of the BitBlazorUI components.
    /// </summary>
    /// <param name="services">The service collection to add the services to.</param>
    /// <param name="trySingleton">
    /// Tries to register the eligible services as singleton instead of scoped.
    /// Only enable this for single-user hosting models (Blazor WebAssembly and Hybrid/MAUI).
    /// </param>
    /// <returns></returns>
    public static IServiceCollection AddBitBlazorUILegacyServices(this IServiceCollection services, bool trySingleton = false)
    {
        services.AddBitBlazorUIServices(trySingleton);

        services.TryAddScoped<BitMarkdownServiceLegacy>();

        return services;
    }
}
