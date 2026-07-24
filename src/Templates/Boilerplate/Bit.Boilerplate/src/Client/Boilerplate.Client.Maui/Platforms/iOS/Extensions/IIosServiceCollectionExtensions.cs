//+:cnd:noEmit
//#if (notification == true)
using Boilerplate.Client.Maui.Platforms.iOS.Services;
//#endif

namespace Microsoft.Extensions.DependencyInjection;

public static partial class IIosServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddClientMauiProjectIosServices(IConfiguration configuration)
        {
            // Services registered in this class can be injected in iOS.

            //#if (notification == true)
            services.AddSingleton<IPushNotificationService, iOSPushNotificationService>();
            //#endif

            return services;
        }
    }
}
