using Bit.BlazorUI.Demo.Shared.Services;

namespace Microsoft.Extensions.DependencyInjection;

public static class IServiceCollectionExtensions
{
    public static void AddSharedServices(this IServiceCollection services)
    {
        // Services being registered here can get injected everywhere (API, Web, Android, iOS, Windows, Mac, and Linux)

        services.AddLocalization();

        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
    }
}
