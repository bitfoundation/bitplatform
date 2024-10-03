using Boilerplate.Client.Maui.Platforms.Android.Services;

namespace Microsoft.Extensions.DependencyInjection;

public static partial class IServiceCollectionExtensions
{
    public static IServiceCollection AddClientMauiProjectAndroidServices(this IServiceCollection services)
    {
        // Services being registered here can get injected in Maui/Android.

        services.TryAddSessioned<IPushNotificationService, AndroidPushNotificationService>();

        return services;
    }
}
