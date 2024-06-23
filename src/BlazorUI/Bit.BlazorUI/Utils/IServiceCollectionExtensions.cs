using Microsoft.Extensions.DependencyInjection;

namespace Bit.BlazorUI;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddBitBlazorUIServices(this IServiceCollection services)
    {
        services.AddScoped<BitThemeManager>();

        return services;
    }
}
