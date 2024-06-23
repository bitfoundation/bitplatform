using Bit.Websites.Platform.Shared.Services;

namespace Microsoft.Extensions.DependencyInjection;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddSharedServices(this IServiceCollection services)
    {
        // Services being registered here can get injected everywhere (Api, Web, Android, iOS, Windows, macOS and Linux)

        services.AddTransient<IDateTimeProvider, DateTimeProvider>();

        return services;
    }
}
