using Microsoft.Extensions.DependencyInjection;

namespace Bit.Butil;

public static class BitButil
{
    public static IServiceCollection AddBitButilServices(this IServiceCollection services)
    {
        services.AddScoped<Window>();
        services.AddScoped<Document>();
        services.AddScoped<Keyboard>();
        services.AddScoped<Console>();
        services.AddScoped<History>();
        services.AddScoped<Element>();
        services.AddScoped<Navigator>();
        services.AddScoped<LocalStorage>();
        services.AddScoped<SessionStorage>();
        services.AddScoped<Location>();
        services.AddScoped<Screen>();
        services.AddScoped<Cookie>();

        return services;
    }
}
