// The host of the demo: a Blazor Web App that prerenders the client's pages and then runs them in the
// browser on WebAssembly (InteractiveWebAssembly), where every database the site opens lives.
//
// It also runs the demo's AppDbContext itself, with the stock UseSqlite provider: the same entities, the same
// configurations and the same migrations as the browser applies through UseBitBsqlite, which is the claim the
// demo exists to show.

using Bit.Bsqlite.Demo.Data;
using Bit.Bsqlite.Demo.Server.Components;
using Bit.Bsqlite.Demo.Server.Controllers;
using Bit.Bsqlite.Demo.Server.Services;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Protocol;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

// The prerender pass instantiates the client's pages in this container, so it registers the very same
// services the WebAssembly container does - Bsqlite's included. Nothing browser-bound runs here: every page
// opens its database only once it is interactive.
builder.Services.AddDemoServices();

// The MCP server (Controllers/McpController.cs) and the plain HTTP endpoints that mirror it - the same
// methods, reachable from a browser, which is what the /mcp-server demo page calls to show them live. That
// page's route has to differ from MapMcp's below: two literal endpoints on /mcp would make a GET of it
// ambiguous. A member with nothing in it is left out of the JSON, exactly as the MCP transport leaves it out,
// so the browser view of a tool is the answer an agent gets rather than that answer plus a column of nulls.
builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull);

builder.Services.AddMcpServer(options =>
{
    // The name a person sees in their client's server list, and the version they would quote in a bug
    // report - the library's, not this web app's, because the library is what the answers are about.
    options.ServerInfo = new Implementation
    {
        Name = "bit-bsqlite",
        Title = "Bit.Bsqlite",
        Version = BsqliteServerInstructions.BsqliteVersion,
        Description = "Documentation, public API, storage backends, setup guides, a storage-selection planner and a SQL guard check for Bit.Bsqlite, SQLite in the browser for Blazor with EF Core on top.",
        WebsiteUrl = "https://github.com/bitfoundation/bitplatform/tree/develop/src/Bsqlite"
    };

    // Returned from `initialize` and put in front of the model before it has called anything.
    options.ServerInstructions = BsqliteServerInstructions.Text;
})
    .WithHttpTransport()
    .WithToolsFromAssembly()
    .WithResourcesFromAssembly()
    .WithPromptsFromAssembly()
    // Every key this server takes - a document name, a type name, a backend, a scenario, a source path - comes
    // from a closed set nothing on the wire spells out; completions let a client type ahead into it.
    .WithCompleteHandler((request, cancellationToken) => ValueTask.FromResult(new CompleteResult
    {
        Completion = BsqliteCompletions.Complete(request.Params?.Argument?.Name, request.Params?.Argument?.Value)
    }))
    // Additive to the attributed resources, not a replacement for them: a template cannot be listed as
    // something to click, so the documents and the setup guides are enumerated one by one as well.
    .WithListResourcesHandler((request, cancellationToken) => ValueTask.FromResult(new ListResourcesResult
    {
        Resources = [.. McpResources.ListDocuments()]
    }));

// The server's copy of the catalog, in memory, so the deployed demo writes nothing to its server's disk and a
// restart starts it over. A named shared-cache database lives as long as one connection to it is open: this
// one is held for the lifetime of the app, and every context opens a connection of its own next to it, so
// concurrent requests never share one SqliteConnection.
const string serverDatabase = "Data Source=bsqlite-demo;Mode=Memory;Cache=Shared";
var keepAlive = new SqliteConnection(serverDatabase);
keepAlive.Open();
builder.Services.AddSingleton(keepAlive);
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(serverDatabase));

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.All;
    options.ForwardedHostHeaderName = "X-Host";
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
    options.ForwardLimit = 1;
});

var app = builder.Build();

// The migrations the browser applies are the ones applied here, by the stock provider.
await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    await DemoData.SeedIfEmptyAsync(db);
}

if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

// A deep link to an unknown route matches no Razor component endpoint, so routing answers with an empty 404.
// Re-execute it through the app to get the page the router shows for the same miss during client-side
// navigation - keeping the status code at 404.
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

app.UseForwardedHeaders();

app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();

// Both are literal routes, so they are matched before any component route regardless of order; declaring
// them first says so out loud.
app.MapControllers();
app.MapMcp("/mcp");

// The server side of the shared model: the catalog the stock provider holds, through the same AppDbContext.
// Read-only: the site is public and the database lives in this process's memory for its whole lifetime, so an
// endpoint that let anyone add rows would let anyone grow it until the process runs out of memory.
app.MapGet("/api/products", async (AppDbContext db, CancellationToken ct) =>
    await db.Products.AsNoTracking().OrderBy(p => p.Id).ToListAsync(ct));

app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Bit.Bsqlite.Demo.Client._Imports).Assembly);

app.Run();

/// <summary>
/// Named so the tests can host this exact file in-memory. Top-level statements compile to an internal
/// Program, which WebApplicationFactory cannot reach from another assembly on its own.
/// </summary>
public partial class Program;
