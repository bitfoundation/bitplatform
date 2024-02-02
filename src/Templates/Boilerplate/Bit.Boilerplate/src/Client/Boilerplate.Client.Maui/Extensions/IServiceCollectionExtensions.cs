//-:cnd:noEmit
using Boilerplate.Client.Maui;
using Boilerplate.Client.Maui.Services;

namespace Microsoft.Extensions.DependencyInjection;

public static partial class IServiceCollectionExtensions
{
    public static IServiceCollection AddClientMauiServices(this IServiceCollection services)
    {
        // Services registered in this class can be injected in Android, iOS, Windows, and macOS.

        services.TryAddTransient<MainPage>();
        services.TryAddTransient<IStorageService, MauiStorageService>();
        services.TryAddSingleton<IBitDeviceCoordinator, MauiDeviceCoordinator>();
        services.TryAddTransient<IExceptionHandler, MauiExceptionHandler>();

#if ANDROID
        services.AddClientAndroidServices();
#elif iOS
        services.AddClientiOSServices();
#elif Mac
        services.AddClientMacServices();
#elif Windows
        services.AddClientWindowsServices();
#endif

        services.AddClientSharedServices();

        return services;
    }
}
