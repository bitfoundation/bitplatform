using Boilerplate.Client.Maui.Platforms.iOS.Services;

namespace Microsoft.Extensions.DependencyInjection;

public static partial class IServiceCollectionExtensions
{
    public static IServiceCollection AddClientMauiProjectIosServices(this IServiceCollection services)
    {
        // Services registered in this class can be injected in iOS.

        services.TryAddSessioned<IPushNotificationService, iOSPushNotificationService>();

        return services;
    }
}
