using Microsoft.Extensions.DependencyInjection;

namespace Bit.Butil;

public static class BitButil
{
    public static IServiceCollection AddBitButilServices(this IServiceCollection services)
    {
        services.AddTransient<Window>();
        services.AddTransient<Document>();
        services.AddTransient<Keyboard>();
        services.AddTransient<Console>();
        services.AddTransient<History>();
        services.AddTransient<Element>();
        services.AddTransient<Navigator>();
        services.AddTransient<LocalStorage>();
        services.AddTransient<SessionStorage>();
        services.AddTransient<Location>();
        services.AddTransient<Screen>();
        services.AddTransient<Cookie>();

        return services;
    }
}
