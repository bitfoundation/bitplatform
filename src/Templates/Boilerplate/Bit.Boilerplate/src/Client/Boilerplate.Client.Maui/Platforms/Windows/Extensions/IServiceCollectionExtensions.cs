//+:cnd:noEmit
//#if (notification == true)
using Boilerplate.Client.Maui.Platforms.Windows.Services;
//#endif

namespace Microsoft.Extensions.DependencyInjection;

public static partial class IServiceCollectionExtensions
{
    public static IServiceCollection AddClientMauiProjectWindowsServices(this IServiceCollection services)
    {
        // Services being registered here can get injected in Maui/windows.

        //#if (notification == true)
        services.TryAddSessioned<IPushNotificationService, WindowsPushNotificationService>();
        //#endif

        return services;
    }
}
