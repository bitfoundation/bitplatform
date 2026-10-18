namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// The services shared by the WebAssembly client and the server host. Both containers have to
/// register the same set: every page is prerendered on the server before it runs in the browser,
/// so anything a page injects must resolve in both.
/// </summary>
public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddDemoServices(this IServiceCollection services)
    {
        // BbScanner needs no registration of its own, and neither do the demo's pages: the scanner
        // loads its JavaScript module itself the first time it renders in the browser, and the
        // sample barcodes are generated in memory. This is where an app would add its own services.
        return services;
    }
}
