//+:cnd:noEmit
//#if (notification == true)
using Boilerplate.Client.Maui.Platforms.Windows.Services;
//#endif

namespace Microsoft.Extensions.DependencyInjection;

public static partial class IWindowsServiceCollectionExtensions
{
    public static IServiceCollection AddClientMauiProjectWindowsServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Services being registered here can get injected in Maui/windows.

        //#if (notification == true)
        services.AddSingleton<IPushNotificationService, WindowsPushNotificationService>();
        //#endif

        return services;
    }
}
