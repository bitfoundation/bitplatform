using Bit.Websites.Platform.Web.Services;

namespace Microsoft.Extensions.DependencyInjection;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddPlaygroundServices(this IServiceCollection services)
    {
        services.AddScoped<NavManuService>();
        return services;
    }
}
