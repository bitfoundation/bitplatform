using Bit.Bmage;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// The services shared by the WebAssembly client and the prerendering host.
/// Both containers have to register the same set: every component is instantiated once on the
/// server for the prerender pass and again in the browser, so anything a page injects must
/// resolve in both places or prerendering fails with a missing-service exception.
/// </summary>
public static class IServiceCollectionExtensions
{
    /// <param name="baseAddress">
    /// The app's own origin, for the <see cref="HttpClient"/> a page can call the host's endpoints
    /// with. The prerendering host passes nothing: it registers the client so an injection resolves,
    /// and a page only ever issues a request from a button, which cannot happen during prerendering.
    /// </param>
    public static IServiceCollection AddDemoServices(this IServiceCollection services, string? baseAddress = null)
    {
        // One call wires up capability detection, loading, processing and export. It is safe to make
        // on the prerendering host too: the services only reach for the browser when a page asks
        // them to do something, and the capability probe answers "Prerendering" rather than throwing
        // when there is no browser to ask.
        services.AddBitBmageServices(options =>
        {
            // Generous for a demo, where people drop whatever is on their phone; a real app would
            // tune these to its users' devices (see the Hosting page).
            options.MaxSourceBytes = 64 * 1024 * 1024;
            options.Limits = ImageLimits.Default with { MaxPixels = 80_000_000 };
        });

        services.AddScoped(_ => baseAddress is null
            ? new HttpClient()
            : new HttpClient { BaseAddress = new Uri(baseAddress) });

        return services;
    }
}
