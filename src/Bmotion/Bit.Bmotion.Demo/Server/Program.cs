using Bit.Bmotion.Demo.Server.Components;

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
