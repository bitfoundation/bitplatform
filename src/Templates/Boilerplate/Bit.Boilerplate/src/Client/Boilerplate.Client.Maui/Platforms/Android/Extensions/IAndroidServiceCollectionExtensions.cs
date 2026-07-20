//+:cnd:noEmit
//#if (notification == true)
using Boilerplate.Client.Maui.Platforms.Android.Services;
//#endif

namespace Microsoft.Extensions.DependencyInjection;

public static partial class IAndroidServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddClientMauiProjectAndroidServices(IConfiguration configuration)
        {
            // Services being registered here can get injected in Maui/Android.

            //#if (notification == true)
            services.AddSingleton<IPushNotificationService, AndroidPushNotificationService>();
            //#endif

            return services;
        }
    }
}
