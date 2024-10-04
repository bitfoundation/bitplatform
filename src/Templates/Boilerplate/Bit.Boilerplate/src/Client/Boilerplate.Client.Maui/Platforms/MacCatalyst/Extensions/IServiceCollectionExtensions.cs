//#if (notification == true)
using Boilerplate.Client.Maui.Platforms.MacCatalyst.Services;
//#endif

namespace Microsoft.Extensions.DependencyInjection;

public static partial class IServiceCollectionExtensions
{
    public static IServiceCollection AddClientMauiProjectMacCatalystServices(this IServiceCollection services)
    {
        // Services being registered here can get injected in Maui/macOS.

        //#if (notification == true)
        services.TryAddSessioned<IPushNotificationService, MacCatalystPushNotificationService>();
        //#endif

        return services;
    }
}
