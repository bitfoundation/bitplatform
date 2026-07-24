//+:cnd:noEmit
//#if (notification == true)
using Boilerplate.Client.Maui.Platforms.MacCatalyst.Services;
//#endif

namespace Microsoft.Extensions.DependencyInjection;

public static partial class IMacServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddClientMauiProjectMacCatalystServices(IConfiguration configuration)
        {
            // Services being registered here can get injected in Maui/macOS.

            //#if (notification == true)
            services.AddSingleton<IPushNotificationService, MacCatalystPushNotificationService>();
            //#endif

            return services;
        }
    }
}
