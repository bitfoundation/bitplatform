using Microsoft.Extensions.DependencyInjection;

namespace Bit.Butil;

public static class BitButil
{
    public static IServiceCollection AddBitButilServices(this IServiceCollection services)
    {
        services.AddTransient<Clipboard>();
        services.AddTransient<Console>();
        services.AddTransient<Cookie>();
        services.AddTransient<Crypto>();
        services.AddTransient<Document>();
        services.AddTransient<History>();
        services.AddTransient<Keyboard>();
        services.AddTransient<LocalStorage>();
        services.AddTransient<SessionStorage>();
        services.AddTransient<Location>();
        services.AddTransient<Navigator>();
        services.AddTransient<Notification>();
        services.AddTransient<Screen>();
        services.AddTransient<ScreenOrientation>();
        services.AddTransient<UserAgent>();
        services.AddTransient<VisualViewport>();
        services.AddTransient<Window>();

        return services;
    }
}
