//+:cnd:noEmit
//#if (notification == true)
using Boilerplate.Client.Maui.Platforms.Android.Services;
//#endif

namespace Microsoft.Extensions.DependencyInjection;

public static partial class IServiceCollectionExtensions
{
    public static IServiceCollection AddClientMauiProjectAndroidServices(this IServiceCollection services)
    {
        // Services being registered here can get injected in Maui/Android.

        //#if (notification == true)
        services.TryAddSessioned<IPushNotificationService, AndroidPushNotificationService>();
        //#endif

        return services;
    }
}
