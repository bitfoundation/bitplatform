﻿using Bit.BlazorUI.Demo.Client.App;
using Bit.BlazorUI.Demo.Client.App.Services.Implementations;

namespace Microsoft.Extensions.DependencyInjection;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddClientAppServices(this IServiceCollection services)
    {
        // Services being registered here can get injected in Android, iOS, Windows, and Mac.

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
