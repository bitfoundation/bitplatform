using Bit.BevTools;
using Bit.BevTools.Demo.Client.Extension;
using Bit.BevTools.Demo.Client.Models;
using Bit.BevTools.Demo.Client.Services;
using Bit.BevTools.Demo.Server.Components;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.HttpOverrides;

DemoCulture.Apply();

var builder = WebApplication.CreateBuilder(args);

// ValidateOnBuild is off so the intentionally broken registrations (captive dependency, cycle) that the
// BevTools Services panel demonstrates do not stop the app from starting. ValidateScopes stays on.
builder.Host.UseDefaultServiceProvider(o => o.ValidateOnBuild = false);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddDemoServices();
builder.Services.AddHttpContextAccessor();
// The server calls its own API at the address of the request it serves, never at NavigationManager.BaseUri, which
// ProductApi falls back to in the browser: in an interactive Server circuit that is whatever the browser sent when the
// circuit started, so a visitor could point the server's requests at any host. Without a request there is no client.
builder.Services.AddHttpClient("api").ConfigureHttpClient((services, client) =>
{
    var request = services.GetRequiredService<IHttpContextAccessor>().HttpContext?.Request
        ?? throw new InvalidOperationException("The demo API is called on the server only while it serves a request.");
    client.BaseAddress = new Uri(UriHelper.BuildAbsolute(request.Scheme, request.Host, request.PathBase, "/"));
});

// One line. An application leaves Enabled alone and BevTools turns itself on in Development only; this demo exists to
// show BevTools, wherever it is deployed (bevtools.bitplatform.dev included), so it is on in every environment.
// Outside Development a process that serves many users reads no application values unless told to; this demo's data
// is made up and shown on purpose, so it says so. An application in production would keep that default.
builder.Services.AddBitBevToolsServerServices(o =>
{
    o.Enabled = true;
    o.ValueAccess = BevValueAccess.Full;
    // The same extension the client registers, so its panel, rule and commands are there on either runtime.
    o.Extensions.Add(new DemoBevToolsExtension());
});

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
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

// A Content Security Policy that forbids frames: the site embeds none, and the Errors page's "Embed a frame the CSP
// forbids" gives BevTools a real violation to report. It carries Blazor's own frame-ancestors directive too (switched off
// below), so the response has one policy header: a browser that keeps only one of several would otherwise drop a part.
app.Use((context, next) =>
{
    context.Response.Headers.ContentSecurityPolicy = "frame-src 'none'; frame-ancestors 'self'";
    return next(context);
});

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseForwardedHeaders();
app.UseHttpsRedirection();
app.UseAntiforgery();
app.MapStaticAssets();

// ----- Demo API used by both render modes -----
var catalog = DemoCatalog.Products;
var api = app.MapGroup("/api");
api.MapGet("/products", () => catalog);
api.MapGet("/search", (string? q) =>
{
    var query = q ?? "";
    return catalog.Where(p => p.Name.Contains(query, StringComparison.OrdinalIgnoreCase) || p.Category.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();
});
api.MapGet("/orders", async (int? delay) =>
{
    await Task.Delay(delay is > 0 ? Math.Min(delay.Value, 10_000) : Random.Shared.Next(1200, 2600));
    return DemoCatalog.Orders;
});
api.MapGet("/orders/{id:int}", async (int id) =>
{
    await Task.Delay(Random.Shared.Next(40, 160));
    return DemoCatalog.Orders.Any(o => o.Id == id)
        ? Results.Ok(new OrderDetails(id, 1 + id % 5, id % 3 == 0 ? "express" : "standard"))
        : Results.NotFound();
});
api.MapGet("/flaky", () => Random.Shared.Next(3) == 0 ? Results.Ok("ok") : Results.Problem("The flaky endpoint failed (simulated).", statusCode: 500));
api.MapGet("/profile", () => new Profile("ada.lovelace", "ada@example.com", "secret-access-token-from-api", "Pro"));

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode(o => o.ContentSecurityFrameAncestorsPolicy = null)
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Bit.BevTools.Demo.Client._Imports).Assembly);

app.Run();

internal static class DemoCatalog
{
    // Declared first: static initializers run in declaration order.
    private static readonly string[] Adjectives = ["Quantum", "Rustic", "Sleek", "Vintage", "Nimble", "Bold", "Silent", "Bright"];
    private static readonly string[] Nouns = ["Lamp", "Keyboard", "Chair", "Bottle", "Headset", "Notebook", "Backpack", "Monitor", "Speaker"];
    private static readonly string[] Categories = ["Office", "Audio", "Furniture", "Outdoor", "Accessories"];
    private static readonly string[] Customers = ["Grace Hopper", "Alan Turing", "Katherine Johnson", "Linus Torvalds", "Margaret Hamilton", "Dennis Ritchie"];
    private static readonly string[] Statuses = ["Pending", "Shipped", "Delivered", "Cancelled"];

    public static readonly List<Product> Products = Enumerable.Range(1, 120).Select(i => new Product(
        i,
        $"{Adjectives[i % Adjectives.Length]} {Nouns[i % Nouns.Length]} {i}",
        Categories[i % Categories.Length],
        Math.Round(9.99m + (i * 37 % 900), 2),
        i * 13 % 50,
        Math.Round(2.5 + (i * 7 % 25) / 10.0, 1))).ToList();

    public static readonly List<Order> Orders = Enumerable.Range(1, 25).Select(i => new Order(
        1000 + i,
        Customers[i % Customers.Length],
        Math.Round(20m + (i * 53 % 700), 2),
        Statuses[i % Statuses.Length],
        DateTimeOffset.UtcNow.AddHours(-i * 3))).ToList();
}
