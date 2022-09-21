using AdminPanel.Shared.Services.Implementations;

namespace Microsoft.Extensions.DependencyInjection;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddSharedServices(this IServiceCollection services)
    {
        // Services being registered here can get injected everywhere (Api, Web, Android, iOS, Windows, and Mac)

        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();

        services.AddAuthorizationCore();

        services.AddLocalization();

        return services;
    }
}
