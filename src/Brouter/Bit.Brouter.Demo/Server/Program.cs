using Bit.Brouter.Demo.Server.Components;
using Bit.Brouter.Demo.Server.Controllers;
using Bit.Brouter.Demo.Server.Services;
using Microsoft.AspNetCore.Components.Web;
using ModelContextProtocol.Protocol;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

// The prerender pass instantiates the client's components in this container, so it has to
// register the very same services the WebAssembly container does.
builder.Services.AddDemoServices();

// The MCP server (Controllers/McpController.cs) and the plain HTTP endpoints that mirror it.
builder.Services.AddControllers();

builder.Services.AddMcpServer(options =>
{
    // The name a person sees in their client's server list, and the version they would quote in a
    // bug report - the library's, not this web app's, because the library is what the answers are about.
    options.ServerInfo = new Implementation
    {
        Name = "bit-brouter",
        Title = "Bit.Brouter",
        Version = BrouterServerInstructions.BrouterVersion,
        Description = "Documentation, public API and route-template analysis for the Bit.Brouter router for Blazor.",
        WebsiteUrl = "https://github.com/bitfoundation/bitplatform/tree/develop/src/Brouter"
    };

    // Returned from `initialize` and put in front of the model before it has called anything, which
    // makes it the only text here that reaches an agent confident enough not to look. See the type.
    options.ServerInstructions = BrouterServerInstructions.Text;
})
    .WithHttpTransport()
    .WithToolsFromAssembly()
    .WithResourcesFromAssembly()
    .WithPromptsFromAssembly()
    // Every argument this server takes is a key into a closed set, and none of those sets is
    // guessable from outside: without this, choosing a docs slug in a client means going and
    // calling a listing tool first, just to learn what a slug looks like.
    .WithCompleteHandler((request, cancellationToken) => ValueTask.FromResult(new CompleteResult
    {
        Completion = BrouterCompletions.Complete(request.Params?.Argument?.Name, request.Params?.Argument?.Value)
    }))
    // Additive to the attributed resources, not a replacement for them: a template cannot be listed
    // as something to click, so the documentation pages are enumerated one by one as well.
    .WithListResourcesHandler((request, cancellationToken) => ValueTask.FromResult(new ListResourcesResult
    {
        Resources = [.. McpResources.ListDocumentationPages()]
    }));

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
