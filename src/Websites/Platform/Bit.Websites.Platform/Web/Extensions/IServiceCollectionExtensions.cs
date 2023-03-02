using Bit.Websites.Platform.Web.Services;

namespace Microsoft.Extensions.DependencyInjection;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddBitPlatformServices(this IServiceCollection services)
    {
        services.AddScoped<NavManuService>();
        return services;
    }
}
