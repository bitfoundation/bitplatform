using Bit.Belemetry.Demo.Client.Services;

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
    /// The app's own origin, for the <see cref="HttpClient"/> the pages call the demo's API with
    /// (the live telemetry feed, the reflected API tables, the MCP page). The prerendering host
    /// passes nothing: it registers the client so the injection resolves, and replaces the pieces
    /// that would otherwise have to call itself over HTTP.
    /// </param>
    public static IServiceCollection AddDemoServices(this IServiceCollection services, string? baseAddress = null)
    {
        // Belemetry's configuration that is the same on both sides: the service identity, the
        // opt-in consent default, the demo's own ActivitySource and Meter, and the URLs that are
        // the demo talking to itself rather than anything worth recording. Each host adds its own
        // exporter on top (the client in Program.cs, the server in Server/Program.cs).
        services.AddBitBelemetryServices(DemoTelemetry.ConfigureShared);

        services.AddScoped(_ => baseAddress is null
            ? new HttpClient()
            : new HttpClient { BaseAddress = new Uri(baseAddress) });

        services.AddScoped<IApiReference, HttpApiReference>();
        services.AddScoped<TelemetryFeedClient>();

        return services;
    }
}
