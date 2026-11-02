namespace Microsoft.Extensions.DependencyInjection;

public static class IServiceCollectionExtensions
{
    /// <summary>
    /// The services of the shared pages. BbScanner itself registers nothing: the component only takes the framework's
    /// IJSRuntime, so every host calls this one method and nothing BbScanner-specific.
    /// </summary>
    public static IServiceCollection AddCoreServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Services registered in this class can be injected in client side (Web, Android, iOS, Windows, macOS)

        return services;
    }
}
