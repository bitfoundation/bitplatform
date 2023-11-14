﻿using Boilerplate.Client.Web.Services;

namespace Microsoft.Extensions.DependencyInjection;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddClientWebServices(this IServiceCollection services)
    {
        // Services being registered here can get injected in web (blazor web assembly & blazor server)
        services.AddScoped<IBitDeviceCoordinator, WebDeviceCoordinator>();
        services.AddScoped<IExceptionHandler, WebExceptionHandler>();

        return services;
    }
}
