﻿namespace Microsoft.Extensions.DependencyInjection;

public static partial class IServiceCollectionExtensions
{
    public static IServiceCollection AddClientMauiProjectAndroidServices(this IServiceCollection services)
    {
        // Services registered in this class can be injected in Android.

        return services;
    }
}
