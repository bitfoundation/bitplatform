﻿//+:cnd:noEmit
//#if (notification == true)
using Boilerplate.Client.Maui.Platforms.iOS.Services;
//#endif

namespace Microsoft.Extensions.DependencyInjection;

public static partial class IServiceCollectionExtensions
{
    public static IServiceCollection AddClientMauiProjectIosServices(this IServiceCollection services)
    {
        // Services registered in this class can be injected in iOS.

        //#if (notification == true)
        services.AddSessioned<IPushNotificationService, iOSPushNotificationService>();
        //#endif

        return services;
    }
}
