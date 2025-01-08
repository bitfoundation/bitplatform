using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Bit.BlazorUI;

public static class IServiceCollectionExtensions
{
    /// <summary>
    /// Registers required services of the Extras package of the BitBlazorUI components.
    /// </summary>
    /// <param name="services"></param>
    /// <param name="trySingleton">Tries to register the services as singleton, but only for the services that can be singleton (e.g. the services that do not use IJSRuntime).</param>
    /// <returns></returns>
    public static IServiceCollection AddBitBlazorUIExtrasServices(this IServiceCollection services, bool trySingleton = false)
    {
        if (trySingleton)
        {
            services.TryAddSingleton<BitModalService>();
            services.TryAddSingleton<BitMessageBoxService>();
        }
        else
        {
            services.TryAddScoped<BitModalService>();
            services.TryAddScoped<BitMessageBoxService>();
        }

        services.TryAddScoped<BitExtraServices>();

        return services;
    }
}
