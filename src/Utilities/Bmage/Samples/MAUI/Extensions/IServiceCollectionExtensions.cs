namespace Microsoft.Extensions.DependencyInjection;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddMauiServices(this IServiceCollection services)
    {
        // Services registered in this class can be injected in Android, iOS, Windows, and macOS.
        // The pages run in this process and reach the WebView over JS interop, so Bmage reports
        // the BrowserServer environment: the managed codecs where they can, the WebView's for WebP and AVIF.
        services.AddCoreServices();

        return services;
    }
}
