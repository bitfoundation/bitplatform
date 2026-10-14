//+:cnd:noEmit
// [mirror] per platform DI registrations of the maui project - keep in sync with:
// - src/Client/Boilerplate.Client.Maui/Platforms/iOS/Extensions/IIosServiceCollectionExtensions.cs
// - src/Client/Boilerplate.Client.Maui/Platforms/MacCatalyst/Extensions/IMacServiceCollectionExtensions.cs
// - src/Client/Boilerplate.Client.Maui/Platforms/Windows/Extensions/IWindowsServiceCollectionExtensions.cs

using Boilerplate.Client.Maui.Platforms.Android.Services;

namespace Microsoft.Extensions.DependencyInjection;

public static partial class IAndroidServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddClientMauiProjectAndroidServices(IConfiguration configuration)
        {
            // Services being registered here can get injected in Maui/Android.

            services.AddSingleton<ISmsOtpReceiver, AndroidSmsOtpReceiver>();

            //#if (notification == true)
            services.AddSingleton<IPushNotificationService, AndroidPushNotificationService>();
            //#endif

            return services;
        }
    }
}
