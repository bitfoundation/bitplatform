using System.Text.Json.Serialization;
using Bit.Blex.Demo.Server.Components;
using Bit.Blex.Demo.Server.Controllers;
using Bit.Blex.Demo.Server.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.HttpOverrides;
using ModelContextProtocol.Protocol;

// InspectBlexStore runs every inspection in a child process of this executable, so a source that sends Roslyn
// into an endless computation costs a killed worker rather than the server. A worker never builds the web app.
if (args is [BlexStoreInspector.WorkerArgument])
{
    Environment.ExitCode = await BlexStoreInspector.RunWorkerAsync(Console.OpenStandardInput(), Console.OpenStandardOutput(), Console.Error);
    return;
}

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

// The prerender pass instantiates the client's components in this container, so it has to
// register the very same services the WebAssembly container does.
builder.Services.AddDemoServices();

// The MCP server (Controllers/McpController.cs) and the plain HTTP endpoints that mirror it. A member
// with nothing in it is left out of the JSON, exactly as the MCP transport leaves it out, so the browser
// view of a tool is the answer an agent gets rather than that answer plus a column of nulls.
builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull);

builder.Services.AddMcpServer(options =>
{
    // The name a person sees in their client's server list, and the version they would quote in a bug
    // report - the library's, not this web app's, because the library is what the answers are about.
    options.ServerInfo = new Implementation
    {
        Name = "bit-blex",
        Title = "Bit.Blex",
        Version = BlexServerInstructions.BlexVersion,
        Description = "Documentation, public API, generator diagnostics and store inspection for Bit.Blex, reactive state management for Blazor and .NET MAUI.",
        WebsiteUrl = "https://github.com/bitfoundation/bitplatform/tree/develop/src/Utilities/Blex"
    };

    // Returned from `initialize` and put in front of the model before it has called anything.
    options.ServerInstructions = BlexServerInstructions.Text;
})
    .WithHttpTransport()
    .WithToolsFromAssembly()
    .WithResourcesFromAssembly()
    .WithPromptsFromAssembly()
    // Every argument this server takes is a key into a closed set that is not guessable from outside.
    .WithCompleteHandler((request, cancellationToken) => ValueTask.FromResult(new CompleteResult
    {
        Completion = BlexCompletions.Complete(request.Params?.Argument?.Name, request.Params?.Argument?.Value)
    }))
    // Additive to the attributed resources: a template cannot be listed as something to click, so the
    // documentation pages are enumerated one by one as well.
    .WithListResourcesHandler((request, cancellationToken) => ValueTask.FromResult(new ListResourcesResult
    {
        Resources = [.. McpResources.ListDocumentationPages()]
    }));

// Renders a docs page outside of a request's component hierarchy, so its content can be handed to an
// MCP client as text. Scoped: a renderer belongs to the request that asked for the page.
builder.Services.AddScoped<HtmlRenderer>();

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.All;
    options.ForwardedHostHeaderName = "X-Host";
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
    options.ForwardLimit = 1;
});

var app = builder.Build();

if (app.Environment.IsDevelopment() is false)
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

// A deep link to an unknown route matches no Razor component endpoint, so routing answers with an
// empty 404. Re-execute it through the app to get the styled page the router shows for the same
// miss during client-side navigation - keeping the status code at 404.
app.UseStatusCodePagesWithReExecute("/not-found");

app.UseForwardedHeaders();

// Locally the MCP server and its HTTP mirror answer on the plain-HTTP port as well, which is the address the docs
// give: a redirect would have an MCP client re-send its POST to https://localhost:7280 - which not every client
// does - and then trust the development certificate. Everything else, and everything in production, is redirected.
app.UseWhen(context => app.Environment.IsDevelopment() is false
                       || (context.Request.Path.StartsWithSegments("/mcp") || context.Request.Path.StartsWithSegments("/api/mcp")) is false,
    https => https.UseHttpsRedirection());

app.UseAntiforgery();

app.MapStaticAssets();

// Both are literal routes, so they are matched before any component route regardless of order;
// declaring them first says so out loud.
app.MapControllers();
app.MapMcp("/mcp");

app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Bit.Blex.Demo.Client._Imports).Assembly);

app.Run();

/// <summary>
/// Named so the tests can host this exact file in-memory. Top-level statements compile to an internal
/// Program, which WebApplicationFactory cannot reach from another assembly.
/// </summary>
public partial class Program;
