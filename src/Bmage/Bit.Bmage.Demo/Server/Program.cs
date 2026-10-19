using Bit.Bmage.Demo.Server.Components;
using Bit.Bmage.Demo.Server.Services;
using Microsoft.AspNetCore.HttpOverrides;
using ModelContextProtocol.Protocol;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

// The prerender pass instantiates the client's components in this container, so it has to
// register the very same services the WebAssembly container does - Bmage's included. Nothing
// browser-bound runs here: the capability probe answers "Prerendering" instead of calling into a
// browser that does not exist yet, and every page does its image work from an event handler or
// OnAfterRenderAsync, which a prerender never reaches.
builder.Services.AddDemoServices();

// The MCP server (Controllers/McpController.cs) and the plain HTTP endpoints that mirror it - the
// same methods, reachable from a browser, which is what the /mcp-server demo page calls to show them
// live. That page's route has to differ from MapMcp's below: two literal endpoints on /mcp would
// make a GET of it ambiguous. A member with nothing in it is left out of the JSON, exactly as the
// MCP transport leaves it out, so the browser view of a tool is the answer an agent gets.
builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull);

builder.Services.AddMcpServer(options =>
{
    // The name a person sees in their client's server list, and the version they would quote in a
    // bug report - the library's, not this web app's, because the library is what the answers are about.
    options.ServerInfo = new Implementation
    {
        Name = "bit-bmage",
        Title = "Bit.Bmage",
        Version = BmageServerInstructions.BmageVersion,
        Description = "Documentation, public API, recipes, code review and a live engine for Bit.Bmage, image processing for Blazor.",
        WebsiteUrl = "https://github.com/bitfoundation/bitplatform/tree/develop/src/Bmage"
    };

    // Returned from `initialize` and put in front of the model before it has called anything, which
    // makes it the only text here that reaches an agent confident enough not to look. See the type.
    options.ServerInstructions = BmageServerInstructions.Text;
})
    .WithHttpTransport()
    .WithToolsFromAssembly()
    .WithResourcesFromAssembly()
    .WithPromptsFromAssembly()
    // Every argument a prompt or resource template takes here is a key into a closed set - a guide
    // name, a type name, a recipe id, a source path - and none of those sets is guessable from outside.
    .WithCompleteHandler((request, cancellationToken) => ValueTask.FromResult(new CompleteResult
    {
        Completion = BmageCompletions.Complete(request.Params?.Argument?.Name, request.Params?.Argument?.Value)
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
    .AddAdditionalAssemblies(typeof(Bit.Bmage.Demo.Client._Imports).Assembly);

app.Run();

/// <summary>
/// Named so the tests can host this exact file in-memory. Top-level statements compile to an
/// internal Program, which WebApplicationFactory cannot reach - and testing a second, hand-written
/// registration of the MCP server instead would leave the wiring above the only part nothing checks.
/// </summary>
public partial class Program;
