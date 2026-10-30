using Bit.BlazorHeadlessUI.Demo.Client.Core;
using Bit.BlazorHeadlessUI.Demo.Server.Services;
using Bit.BlazorHeadlessUI.Demo.Server.Components;

var builder = WebApplication.CreateBuilder(args);

// Blazor Server is always registered: Debug builds render in it (see AppRenderMode), and App.razor falls
// back to it whenever the WASM client is not part of the build.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
#if INCLUDE_WASM
    .AddInteractiveWebAssemblyComponents()
#endif
    ;

// The prerender pass (and Blazor Server rendering) instantiates the client's pages in this container, so
// it registers the very same services the WebAssembly container does.
builder.Services.AddDemoClientServices();

// The MCP server (Controllers/McpController.cs) and the plain HTTP GET endpoints that mirror its tools
// under /api/mcp/...
builder.Services.AddControllers();
builder.Services.AddMcpServer(options =>
{
    options.ServerInfo = new ModelContextProtocol.Protocol.Implementation
    {
        Name = "bit-blazorheadlessui",
        Title = "bit BlazorHeadlessUI - accessible, unstyled Blazor components",
        Version = BlazorHeadlessUIComponentCatalog.Version,
        WebsiteUrl = "https://github.com/bitfoundation/bitplatform/tree/develop/src/BlazorHeadlessUI",
    };

    // The one text a server writes straight into the model's context, before any call: which tool to
    // reach for first and the few rules no single tool owns.
    options.ServerInstructions = BlazorHeadlessUIMcpInstructions.Text;
})
    .WithHttpTransport()
    .WithToolsFromAssembly();

var app = builder.Build();

if (app.Environment.IsDevelopment() is false)
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// A deep link to an unknown route matches no endpoint, so routing answers with an empty 404. Re-execute
// it through the app to get the page the router shows for the same miss during client-side navigation
// (Routes.razor's NotFoundPage) - keeping the status code at 404.
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();

// Literal routes, matched before any component route regardless of order; declared first to say so.
app.MapControllers();
app.MapMcp("/mcp");

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
#if INCLUDE_WASM
    .AddInteractiveWebAssemblyRenderMode()
#endif
    .AddAdditionalAssemblies(typeof(Routes).Assembly);

// Building the catalog reflects the library and parses every demo page, which the first MCP client to
// connect would otherwise wait for. Off the startup path, and a catalog that cannot be built costs one
// tool call rather than the app.
_ = Task.Run(() =>
{
    try
    {
        BlazorHeadlessUISearchIndex.Warm();
    }
    catch (Exception exception)
    {
        app.Logger.LogWarning(exception, "The MCP catalog could not be warmed at startup.");
    }
});

app.Run();

/// <summary>
/// Named so the tests can host this exact file in memory (WebApplicationFactory): top-level statements
/// compile to an internal Program, and a second registration of the MCP server written for the tests
/// would leave the wiring above the only part nothing checks.
/// </summary>
public partial class Program;
