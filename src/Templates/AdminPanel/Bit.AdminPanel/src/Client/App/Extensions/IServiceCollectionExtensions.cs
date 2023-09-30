//-:cnd:noEmit
using AdminPanel.Client.App;
using AdminPanel.Client.App.Services;

namespace Microsoft.Extensions.DependencyInjection;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddClientAppServices(this IServiceCollection services)
    {
        // Services registered in this class can be injected in Android, iOS, Windows, and macOS.

#if ANDROID
        services.AddClientAndroidServices();
#elif iOS
        services.AddClientiOSServices();
#elif Mac
        services.AddClientMacServices();
#elif Windows
        services.AddClientWindowsServices();
#endif

        services.AddScoped<MainPage>();
        services.AddSingleton<IBitDeviceCoordinator, AppDeviceCoordinator>();
        services.AddScoped<IExceptionHandler, AppExceptionHandler>();

        return services;
    }
}
