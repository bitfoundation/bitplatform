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
        // BlowCanvas needs no registration of its own, and the demo pages only inject what Blazor
        // already provides (NavigationManager, IJSRuntime). The method is kept so both hosts share
        // one place to add a service the day a page needs one.
        return services;
    }
}
