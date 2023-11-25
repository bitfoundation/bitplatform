using Microsoft.Extensions.DependencyInjection;

namespace Bit.Butil;

public static class BitButil
{
    public static IServiceCollection AddBitButilServices(this IServiceCollection services)
    {
        services.AddScoped<Console>();
        services.AddScoped<Document>();
        services.AddScoped<Window>();

        return services;
    }
}
