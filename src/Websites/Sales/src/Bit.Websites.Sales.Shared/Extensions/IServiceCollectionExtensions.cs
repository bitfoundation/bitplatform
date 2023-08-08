using Bit.Websites.Sales.Shared.Services.Implementations;

namespace Microsoft.Extensions.DependencyInjection;

public static class IServiceCollectionExtensions
{
    public static void AddSharedServices(this IServiceCollection services)
    {
        // Services being registered here can get injected everywhere (Api & Web)

        services.AddLocalization();

        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
    }
}
