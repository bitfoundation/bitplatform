using Microsoft.Extensions.DependencyInjection;

namespace Bit.BlazorUI;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddBitBlazorUIExtrasServices(this IServiceCollection services)
    {
        services.AddScoped<BitModalService>();

        return services;
    }
}
