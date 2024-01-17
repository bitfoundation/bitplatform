using Boilerplate.Client.Windows.Services;

namespace Microsoft.Extensions.DependencyInjection;

public static class IServiceCollectionExtensions
{
    public static void AddWindowsServices(this IServiceCollection services)
    {
        services.TryAddTransient<IStorageService, WindowsStorageService>();
        services.AddTransient<IBitDeviceCoordinator, WindowsDeviceCoordinator>();
        services.AddTransient<IExceptionHandler, WindowsExceptionHandler>();

        services.AddClientSharedServices();
    }
}
