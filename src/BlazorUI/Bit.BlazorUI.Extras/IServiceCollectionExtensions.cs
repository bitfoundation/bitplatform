using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Bit.BlazorUI;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddBitBlazorUIExtrasServices(this IServiceCollection services, bool singleton = false)
    {
        if (singleton)
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
