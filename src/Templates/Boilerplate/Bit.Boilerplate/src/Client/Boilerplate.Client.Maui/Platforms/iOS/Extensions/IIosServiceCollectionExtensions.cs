//+:cnd:noEmit
//#if (notification == true)
using Boilerplate.Client.Maui.Platforms.iOS.Services;
//#endif

namespace Microsoft.Extensions.DependencyInjection;

public static partial class IIosServiceCollectionExtensions
{
    public static IServiceCollection AddClientMauiProjectIosServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Services registered in this class can be injected in iOS.

        //#if (notification == true)
        services.AddSingleton<IPushNotificationService, iOSPushNotificationService>();
        //#endif

        return services;
    }
}
