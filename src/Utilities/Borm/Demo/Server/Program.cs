// The host of the demo: a Blazor Web App that prerenders the client's pages and then runs them in the
// browser on WebAssembly (InteractiveWebAssembly), where every keystroke a form reacts to is handled
// without a network round trip.

using Bit.Borm.Demo.Server.Components;
using Bit.Borm.Demo.Server.Controllers;
using Bit.Borm.Demo.Server.Services;
using Microsoft.AspNetCore.HttpOverrides;
using ModelContextProtocol.Protocol;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

// The same registration as the WebAssembly client, so the components get the same services while
// they are prerendered here.
builder.Services.AddDemoServices();

// The MCP server (Controllers/McpController.cs) and the plain HTTP endpoints that mirror it. A
// member with nothing in it is left out of the JSON, exactly as the MCP transport leaves it out, so
// the browser view of a tool is the answer an agent gets rather than that answer plus a column of nulls.
builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull);

builder.Services.AddMcpServer(options =>
{
    // The name a person sees in their client's server list, and the version they would quote in a
    // bug report - the library's, not this web app's, because the library is what the answers are about.
    options.ServerInfo = new Implementation
    {
        Name = "bit-borm",
        Title = "Bit.Borm",
        Version = BormServerInstructions.BormVersion,
        Description = "Documentation, public API, theme, setup guides and JSON Schema inspection for the Bit.Borm schema-driven form engine for Blazor.",
        WebsiteUrl = "https://github.com/bitfoundation/bitplatform/tree/develop/src/Utilities/Borm"
    };

    // Returned from `initialize` and put in front of the model before it has called anything.
    options.ServerInstructions = BormServerInstructions.Text;
})
    .WithHttpTransport()
    .WithToolsFromAssembly()
    .WithResourcesFromAssembly()
    .WithPromptsFromAssembly()
    // Every key this server takes - a guide heading, a type name, a source path - comes from a closed
    // set nothing on the wire spells out; completions let a client type ahead into it.
    .WithCompleteHandler((request, cancellationToken) => ValueTask.FromResult(new CompleteResult
    {
        Completion = BormCompletions.Complete(request.Params?.Argument?.Name, request.Params?.Argument?.Value)
    }))
    // Additive to the attributed resources, not a replacement for them: a template cannot be listed
    // as something to click, so the guide's top-level sections are enumerated one by one as well.
    .WithListResourcesHandler((request, cancellationToken) => ValueTask.FromResult(new ListResourcesResult
    {
        Resources = [.. McpResources.ListGuideSections()]
    }));

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

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

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
    .AddAdditionalAssemblies(typeof(Bit.Borm.Demo.Client._Imports).Assembly);

app.Run();

/// <summary>
/// Named so the MCP tests can host this exact file in-memory. Top-level statements compile to an
/// internal Program, which WebApplicationFactory cannot reach from another assembly on its own.
/// </summary>
public partial class Program;
