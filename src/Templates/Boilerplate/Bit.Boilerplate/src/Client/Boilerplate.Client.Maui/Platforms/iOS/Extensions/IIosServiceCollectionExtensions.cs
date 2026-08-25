//+:cnd:noEmit
// [mirror] per platform DI registrations of the maui project - keep in sync with:
// - src/Client/Boilerplate.Client.Maui/Platforms/Android/Extensions/IAndroidServiceCollectionExtensions.cs
// - src/Client/Boilerplate.Client.Maui/Platforms/MacCatalyst/Extensions/IMacServiceCollectionExtensions.cs
// - src/Client/Boilerplate.Client.Maui/Platforms/Windows/Extensions/IWindowsServiceCollectionExtensions.cs

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
