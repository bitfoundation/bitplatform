using System.Text.Json;

namespace Boilerplate.LandingPage.Server.Components;

public partial class App
{
    [AutoInject] private LandingServerSettings settings = default!;

    [AutoInject] private IConfiguration configuration = default!;

    [CascadingParameter] private HttpContext HttpContext { get; set; } = default!;

    // Crawlers read the prerendered page and benchmarks measure it, so scripts only cost them
    private bool OmitScripts => HttpContext.Request.IsLightHouseRequest() || HttpContext.Request.IsCrawlerClient();

    // The OTLP endpoint for the WebAssembly runtime, which reads no server settings; the headers stay on the server
    private string WebAssemblyEnvironmentVariables => configuration.GetBrowserOtlpEndpoint() is { } otlp
        ? JsonSerializer.Serialize(new Dictionary<string, string>
        {
            ["OTEL_EXPORTER_OTLP_ENDPOINT"] = otlp.Endpoint.OriginalString,
            ["OTEL_EXPORTER_OTLP_PROTOCOL"] = otlp.Protocol
        })
        : "{}";

    private readonly BitThemeSsrOptions themeOptions = new()
    {
        Persist = true,
        ViewTransition = true,
        ThemeColorMeta = true
    };
}
