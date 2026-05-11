using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Bit.BlazorUI;

public static class IBitBlazorUIServiceCollectionExtensions
{
    public static IServiceCollection AddBitBlazorUIServices(this IServiceCollection services)
    {
        services.TryAddScoped<BitThemeNotifications>();
        services.TryAddScoped<BitThemeJsNotifierReceiver>();
        services.TryAddScoped<BitThemeManager>();
        services.TryAddScoped<BitExternalThemeLoader>();

        services.TryAddScoped<BitPageVisibility>();

        return services;
    }
}
