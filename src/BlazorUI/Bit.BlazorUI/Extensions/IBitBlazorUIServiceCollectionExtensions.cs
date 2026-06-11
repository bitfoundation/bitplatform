using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Bit.BlazorUI;

public static class IBitBlazorUIServiceCollectionExtensions
{
    /// <summary>
    /// Registers the core Bit.BlazorUI services.
    /// </summary>
    public static IServiceCollection AddBitBlazorUIServices(this IServiceCollection services, bool trySingleton = false)
    {
        if (trySingleton)
        {
            services.TryAddSingleton<BitModalService>();
            services.TryAddSingleton<BitThemeManager>();
            services.TryAddSingleton<BitPageVisibility>();
        }
        else
        {
            services.TryAddScoped<BitModalService>();
            services.TryAddScoped<BitThemeManager>();
            services.TryAddScoped<BitPageVisibility>();
        }



        return services;
    }
}
