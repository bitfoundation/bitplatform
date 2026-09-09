using Bit.Bmotion.Demo.Server.Components;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

// The prerender pass instantiates the client's components in this container, so it has to
// register the very same services the WebAssembly container does.
builder.Services.AddDemoServices();

// The MCP server (Controllers/McpController.cs) and the plain HTTP endpoints that mirror it - the
// same methods, reachable from a browser, which is what the /mcp-server demo page calls to show them
// live. That page's route has to differ from MapMcp's below: two literal endpoints on /mcp would
// make a GET of it ambiguous.
builder.Services.AddControllers();
builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithToolsFromAssembly()
    .WithResourcesFromAssembly()
    .WithPromptsFromAssembly();

// The site is reached through a proxy that forwards over plain http. Without this, UseHttpsRedirection
// below answers every request with a redirect to the url it already asked for, and the absolute urls the
// endpoints build come out as http.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.All;
    options.ForwardedHostHeaderName = "X-Host";
    // The proxy is not on loopback, so the default trust lists would ignore the headers outright. A
    // ForwardLimit of 1 is what keeps a client's own entry unreachable, to the left of the one the
    // front end appends.
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
app.UseStatusCodePagesWithReExecute("/not-found");

app.UseForwardedHeaders();

app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();

// Both are literal routes, so they are matched before any component route regardless of order;
// declaring them first says so out loud.
app.MapControllers();
app.MapMcp("/mcp");

app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Bit.Bmotion.Demo.Client._Imports).Assembly);

app.Run();

/// <summary>
/// Named so the tests can host this exact file in-memory. Top-level statements compile to an
/// internal Program, which WebApplicationFactory cannot reach - and testing a second, hand-written
/// registration of the MCP server instead would leave the wiring above the only part nothing checks.
/// </summary>
public partial class Program;
