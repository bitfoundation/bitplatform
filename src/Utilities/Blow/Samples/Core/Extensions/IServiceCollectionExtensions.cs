namespace Microsoft.Extensions.DependencyInjection;

public static class IServiceCollectionExtensions
{
    /// <summary>
    /// The services of the shared pages. Blow itself registers nothing: the canvas only takes the framework's
    /// IJSRuntime, so every host calls this one method and nothing Blow-specific.
    /// </summary>
    public static IServiceCollection AddCoreServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Services registered in this class can be injected in client side (Web, Android, iOS, Windows, macOS)

        return services;
    }
}
