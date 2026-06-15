using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Bit.BlazorUI;

public static class IBitBlazorUIServiceCollectionExtensions
{
    /// <summary>
    /// Registers the core Bit.BlazorUI services.
    /// </summary>
    /// <param name="services"></param>
    /// <param name="trySingleton">Tries to register the services as singleton, but only for the services that can be singleton (e.g. the services that do not use IJSRuntime).</param>
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
