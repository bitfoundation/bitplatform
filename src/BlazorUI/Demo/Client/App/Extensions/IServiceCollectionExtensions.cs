﻿using Bit.BlazorUI.Demo.Client.App.Services;

namespace Microsoft.Extensions.DependencyInjection;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddClientAppServices(this IServiceCollection services)
    {
        // Services being registered here can get injected in Android, iOS, Windows, and Mac.

        services.AddSingleton<IBitDeviceCoordinator, AppDeviceCoordinator>();

#if ANDROID
        services.AddClientAndroidServices();
#elif iOS
        services.AddClientiOSServices();
#elif Mac
        services.AddClientMacServices();
#elif Windows
        services.AddClientWindowsServices();
#endif

        return services;
    }
}
