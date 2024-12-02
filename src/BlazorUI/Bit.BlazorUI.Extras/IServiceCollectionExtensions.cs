using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Bit.BlazorUI;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddBitBlazorUIExtrasServices(this IServiceCollection services, bool trySingleton = false)
    {
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
