using Boilerplate.Client.Maui.Platforms.Windows.Services;

namespace Microsoft.Extensions.DependencyInjection;

public static partial class IServiceCollectionExtensions
{
    public static IServiceCollection AddClientMauiProjectWindowsServices(this IServiceCollection services)
    {
        // Services being registered here can get injected in Maui/windows.

        services.TryAddSessioned<IPushNotificationService, WindowsPushNotificationService>();

        return services;
    }
}
