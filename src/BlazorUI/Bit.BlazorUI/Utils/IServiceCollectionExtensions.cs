using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Bit.BlazorUI;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddBitBlazorUIServices(this IServiceCollection services, bool singleton = false)
    {
        if (singleton)
        {
            services.TryAddSingleton<BitThemeManager>();
        }
        else
        {
            services.TryAddScoped<BitThemeManager>();
        }

        return services;
    }
}
