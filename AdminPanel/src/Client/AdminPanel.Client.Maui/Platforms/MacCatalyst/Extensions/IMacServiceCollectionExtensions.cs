using AdminPanel.Client.Maui.Platforms.MacCatalyst.Services;

namespace Microsoft.Extensions.DependencyInjection;

public static partial class IMacServiceCollectionExtensions
{
    public static IServiceCollection AddClientMauiProjectMacCatalystServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Services being registered here can get injected in Maui/macOS.

        services.AddSessioned<IPushNotificationService, MacCatalystPushNotificationService>();

        return services;
    }
}
