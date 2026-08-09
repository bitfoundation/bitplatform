//+:cnd:noEmit
// [mirror] per platform DI registrations of the maui project - keep in sync with:
// - src/Client/Boilerplate.Client.Maui/Platforms/Android/Extensions/IAndroidServiceCollectionExtensions.cs
// - src/Client/Boilerplate.Client.Maui/Platforms/iOS/Extensions/IIosServiceCollectionExtensions.cs
// - src/Client/Boilerplate.Client.Maui/Platforms/MacCatalyst/Extensions/IMacServiceCollectionExtensions.cs

//#if (notification == true)
using Boilerplate.Client.Maui.Platforms.Windows.Services;
//#endif

namespace Microsoft.Extensions.DependencyInjection;

public static partial class IWindowsServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddClientMauiProjectWindowsServices(IConfiguration configuration)
        {
            // Services being registered here can get injected in Maui/windows.

            //#if (notification == true)
            services.AddSingleton<IPushNotificationService, WindowsPushNotificationService>();
            //#endif

            return services;
        }
    }
}
