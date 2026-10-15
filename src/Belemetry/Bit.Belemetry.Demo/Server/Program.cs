using Bit.Belemetry.Demo.Client.Services;
using Bit.Belemetry.Demo.Server.Components;
using Bit.Belemetry.Demo.Server.Controllers;
using Bit.Belemetry.Demo.Server.Endpoints;
using Bit.Belemetry.Demo.Server.Services;
using Microsoft.AspNetCore.HttpOverrides;
using ModelContextProtocol.Protocol;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

// The prerender pass instantiates the client's components in this container, so it has to
// register the very same services the WebAssembly container does - Belemetry's shared
// configuration (DemoTelemetry.ConfigureShared) included.
builder.Services.AddDemoServices();

// Prerendering answers the API tables from the reflected catalog directly instead of calling itself
// over HTTP. Registered after AddDemoServices, so this one is what the container resolves.
builder.Services.AddScoped<IApiReference, ServerApiReference>();

// The in-process OTLP receiver behind the Live telemetry page. Memory only, bounded per session.
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<TelemetryStore>();

// The server's half of Belemetry. Everything here is added on top of DemoTelemetry.ConfigureShared:
builder.Services.AddBitBelemetryServices(options =>
{
    // The demo exports to itself. The browser script posts to the same origin it was served from
    // (a relative BrowserEndpoint, so it is right behind any proxy and on any host name), and this
    // process's own pipeline posts to its own listening address over loopback.
    options.AddOtlpExporter(DemoEndpoints.SelfOtlpEndpoint(builder.Configuration), otlp =>
    {
        otlp.Protocol = BelemetryOtlpProtocol.HttpJson;
        otlp.BrowserEndpoint = new Uri("/otlp", UriKind.Relative);
    });

    // The server's own request spans, logs and metrics - governed by Server.Features (an operator
    // decision), not by any one reader's consent. They show on the Live telemetry page when they
    // share a trace with the reader's fetch spans.
    options.Server.CollectDotnetTelemetry = true;

    // Only when the deployment is given one: the demo must not ship telemetry to anyone's Azure
    // resource by default.
    if (builder.Configuration["ApplicationInsights:ConnectionString"] is { Length: > 0 } connectionString)
    {
        options.AddAzureMonitorExporter(connectionString);
    }
});

// The MCP server (Controllers/McpController.cs) and the plain HTTP endpoints that mirror it - the
// same methods, reachable from a browser, which is what the /mcp-server demo page calls to show them
// live. That page's route has to differ from MapMcp's below: two literal endpoints on /mcp would
// make a GET of it ambiguous.
builder.Services.AddControllers();
builder.Services.AddMcpServer(options =>
{
    // The name a person sees in their client's server list, and the version they would quote in a bug report -
    // the library's, not this web app's, because the library is what the answers are about.
    options.ServerInfo = new Implementation
    {
        Name = "bit-belemetry",
        Title = "Bit.Belemetry",
        Version = McpController.BelemetryVersion.Split('+')[0],
        Description = "Setup guides, public API, configuration generation and review, and the semantic conventions of the Bit.Belemetry telemetry SDK for Blazor.",
        WebsiteUrl = "https://github.com/bitfoundation/bitplatform/tree/develop/src/Belemetry"
    };
})
    .WithHttpTransport()
    .WithToolsFromAssembly()
    .WithResourcesFromAssembly()
    .WithPromptsFromAssembly();

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.All;
    options.ForwardedHostHeaderName = "X-Host";
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
    options.ForwardLimit = 1;
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

// A deep link to an unknown route matches no Razor component endpoint, so routing answers with an
// empty 404. Re-execute it through the app to get the styled page the router shows for the same
// miss during client-side navigation - keeping the status code at 404.
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

app.UseForwardedHeaders();

// The receiver is posted to over plain HTTP on loopback by this process's own exporter; redirecting
// that to HTTPS would turn every export into a failed redirect.
app.UseWhen(context => context.Request.Path.StartsWithSegments("/otlp") is false, branch => branch.UseHttpsRedirection());

app.UseAntiforgery();

app.MapStaticAssets();

// All literal routes, so they are matched before any component route regardless of order;
// declaring them first says so out loud.
app.MapOtlpReceiver();
app.MapControllers();
app.MapMcp("/mcp");

app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Bit.Belemetry.Demo.Client._Imports).Assembly);

app.Run();

/// <summary>
/// Named so the tests can host this exact file in-memory. Top-level statements compile to an
/// internal Program, which WebApplicationFactory cannot reach - and testing a second, hand-written
/// registration of the MCP server instead would leave the wiring above the only part nothing checks.
/// </summary>
public partial class Program;
