using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Bit.BlazorUI;

public static class IBitBlazorUIServiceCollectionExtensions
{
    public static IServiceCollection AddBitBlazorUIServices(this IServiceCollection services, bool trySingleton = false)
    {
        if (trySingleton)
        {
            services.TryAddSingleton<BitModalService>();
        }
        else
        {
            services.TryAddScoped<BitModalService>();
        }

        services.TryAddScoped<BitThemeManager>();

        services.TryAddScoped<BitPageVisibility>();

        return services;
    }
}
