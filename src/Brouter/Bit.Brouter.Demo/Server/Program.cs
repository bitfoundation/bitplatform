using Bit.Brouter.Demo.Server.Components;
using Microsoft.AspNetCore.Components.Web;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

// The prerender pass instantiates the client's components in this container, so it has to
// register the very same services the WebAssembly container does.
builder.Services.AddDemoServices();

// The MCP server (Controllers/McpController.cs) and the plain HTTP endpoints that mirror it.
builder.Services.AddControllers();
builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithToolsFromAssembly()
    .WithResourcesFromAssembly()
    .WithPromptsFromAssembly();

// Renders a docs page outside of a request's component hierarchy, so its content can be handed to
// an MCP client as text. Scoped: a renderer belongs to the request that asked for the page.
builder.Services.AddScoped<HtmlRenderer>();

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

app.UseHttpsRedirection();
app.UseAntiforgery();

app.MapStaticAssets();

// Both are declared before the catch-all host page below: /api/... and /mcp are literal routes, so
// they win over "/{*path}" regardless of order, but keeping them first says so out loud.
app.MapControllers();
app.MapMcp("/mcp");

// Components/Pages/Host.razor is a catch-all page, so every deep link matches an endpoint and
// gets the prerendered app back - Brouter then resolves the real route (including its own 404).
app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode();

app.Run();
