using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Bit.BlazorUI;

public static class IBitBlazorUIServiceCollectionExtensions
{
    public static IServiceCollection AddBitBlazorUIServices(this IServiceCollection services)
    {
        services.TryAddScoped<BitThemeNotifications>(sp =>
            new BitThemeNotifications(sp.GetService<ILoggerFactory>()));

        // BitThemeJsNotifierReceiver is internal (consumers should listen on
        // BitThemeNotifications.ThemeChanged), but DI still resolves it for us.
        services.TryAddScoped<BitThemeJsNotifierReceiver>(sp =>
            new BitThemeJsNotifierReceiver(
                sp.GetRequiredService<BitThemeNotifications>(),
                sp.GetService<ILoggerFactory>()));

        services.TryAddScoped<BitThemeManager>(sp =>
            new BitThemeManager(
                sp.GetRequiredService<IJSRuntime>(),
                sp.GetRequiredService<BitThemeJsNotifierReceiver>(),
                sp.GetService<ILoggerFactory>()));

        services.TryAddScoped<BitExternalThemeLoader>();

        services.TryAddScoped<BitPageVisibility>();

        return services;
    }
}
