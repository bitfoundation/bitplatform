using Bit.Bonboarding;

namespace Microsoft.Extensions.DependencyInjection;

public static class IServiceCollectionExtensions
{
    /// <summary>
    /// The services the sample pages inject. Register them in every project that renders the pages - in a Blazor Web
    /// App that is both the server and the client project, because a page is prerendered on the server before it
    /// runs in the browser, and under Auto it may run interactively on either side.
    /// </summary>
    public static IServiceCollection AddCoreServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Services registered in this class can be injected in client side (Web, Android, iOS, Windows, macOS)

        // Scoped, so one set per circuit on the server and one per app in the browser and in a BlazorWebView. The
        // default store keeps progress in the browser's localStorage, read through JS interop wherever .NET runs.
        services.AddBitBonboardingServices(options =>
        {
            options.StorageKeyPrefix = "bit-bonboarding-samples:";
        });

        return services;
    }
}
