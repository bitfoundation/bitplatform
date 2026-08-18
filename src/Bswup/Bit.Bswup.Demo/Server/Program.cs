using System.IO.Compression;
using System.Text;
using System.Globalization;
using System.Threading.RateLimiting;
using Bit.Bswup.Demo.Client;
using Bit.Bswup.Demo.Server.Components;
using Bit.Bswup.Demo.Server.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.ResponseCompression;
using ModelContextProtocol.Protocol;

// The rate-limiting policy the MCP endpoints and their HTTP mirror share.
const string McpRateLimiterPolicy = "mcp";

// Requests one caller may make to those endpoints per minute.
const int McpRequestsPerMinute = 240;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

// App.razor reads the "no-prerender" query off HttpContext to honor the service worker's
// escape hatch (see self.noPrerenderQuery in the client's service-worker files).
builder.Services.AddHttpContextAccessor();

// The MCP server (Controllers/McpController.cs) and the plain HTTP endpoints that mirror it.
builder.Services.AddControllers();

// Those two route groups are the only endpoints here that do real work per request - rendering a
// docs page, parsing a service-worker file a caller pasted in - and anyone with the URL can drive
// them in a loop. A per-caller window keeps one agent from being everyone else's outage; the site's
// own pages and static assets are deliberately left out of it. Behind a proxy every caller shares
// the proxy's address, so the window is sized for a shared bucket rather than for one machine.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy(McpRateLimiterPolicy, context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = McpRequestsPerMinute,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        }));

    // Writing the rejection out here is load-bearing, not a courtesy. The default rejection is a
    // bare status with no body, and UseStatusCodePagesWithReExecute below re-executes exactly
    // those through the Blazor app: a throttled GET came back as the 16 KB /not-found page, and a
    // throttled POST - every MCP call and both file-checking endpoints - came back as 400 "The
    // request has an incorrect Content-type." from the antiforgery middleware rejecting the
    // re-executed request. A client that should have backed off saw a malformed-request error
    // instead. A response that carries its own body and content type is left alone.
    options.OnRejected = async (context, cancellationToken) =>
    {
        var response = context.HttpContext.Response;

        response.StatusCode = StatusCodes.Status429TooManyRequests;

        var retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var window)
            ? (int)Math.Ceiling(window.TotalSeconds)
            : 60;

        response.Headers.RetryAfter = retryAfter.ToString(CultureInfo.InvariantCulture);

        await response.WriteAsJsonAsync(
            new
            {
                error = "too_many_requests",
                message = $"This endpoint allows {McpRequestsPerMinute} requests per minute per caller. Retry in {retryAfter} seconds.",
                retryAfterSeconds = retryAfter
            },
            cancellationToken);
    };
});

builder.Services.AddMcpServer(options =>
{
    // What a client shows in its server list and reports in a bug. Left unset, this is the
    // assembly's name and its 1.0.0.0 file version, which identifies nothing.
    options.ServerInfo = new Implementation
    {
        Name = "bit-bswup",
        Title = "bit Bswup",
        Version = BswupScriptCatalog.Version,
        WebsiteUrl = SiteMetadata.Origin
    };

    // The one piece of text a client is expected to put in front of the model before it has called
    // anything. Deliberately not a summary of the tools - the client already has every tool's
    // description - but the things an agent gets wrong when nothing tells it otherwise: answering
    // about Bswup from memory, and configuring only one of the two service-worker files.
    options.ServerInstructions = @"This server answers about bit Bswup, the service-worker layer for Blazor WebAssembly apps
(offline support, an install progress bar, controlled updates).

Answer from these tools rather than from memory. Bswup's setting names look like Workbox's and like
the standard Microsoft Blazor PWA template's but are not the same, and its defaults have changed
between versions - BswupProgress.AutoReload flipped to false in v-10-6-0. Every tool here reads the
shipped build, so it is right about the version in front of you where recalled knowledge is not.

Start with SearchBswup unless you already know the setting, slug or event you want; each hit names
the exact follow-up call.

Two rules decide most Bswup bugs, and neither produces an error anyone sees until a user is offline:
every self.* setting must be assigned BEFORE the importScripts line in service-worker.js, and
whatever goes in service-worker.js must go in service-worker.published.js too, because the published
file is what deployed builds ship. After writing or changing either file, run it through
InspectBswupServiceWorker and confirm what it caches with AnalyzeBswupAssetCaching before reporting
the work as done.";

    // Advertised explicitly: the SDK only derives completions from enum-valued schemas, and the
    // values worth completing here (docs slugs, guide headings, source paths) are catalog entries.
    options.Capabilities ??= new ServerCapabilities();
    options.Capabilities.Completions = new CompletionsCapability();
    options.Handlers.CompleteHandler = BswupCompletions.CompleteAsync;
})
    .WithHttpTransport()
    .WithToolsFromAssembly()
    .WithResourcesFromAssembly()
    .WithPromptsFromAssembly();

// Renders a docs page outside of a request's component hierarchy, so its content can be handed to
// an MCP client as text. Scoped: a renderer belongs to the request that asked for the page.
builder.Services.AddScoped<HtmlRenderer>();

// The MCP explorer page (Client/Pages/McpPage.razor) injects an HttpClient. It only ever calls one
// after the browser has taken over, but the component is also prerendered here - and rendered here
// again when an MCP client asks for that page - so the dependency has to resolve in this container
// too, or both of those fail with nothing rendered.
builder.Services.AddScoped(_ => new HttpClient());

builder.Services.AddResponseCompression(opts =>
{
    opts.EnableForHttps = true;
    opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(["application/octet-stream"]);
    opts.Providers.Add<BrotliCompressionProvider>();
    opts.Providers.Add<GzipCompressionProvider>();
})
    .Configure<BrotliCompressionProviderOptions>(opt => opt.Level = CompressionLevel.Fastest)
    .Configure<GzipCompressionProviderOptions>(opt => opt.Level = CompressionLevel.Fastest);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
    app.UseResponseCompression();
}

app.UseHttpsRedirection();

// A deep link that matches no page (/typo, and likewise a missing static file) never reaches
// Blazor: routing answers it with an empty 404 before MapRazorComponents is consulted, so the
// router's <NotFound> fragment - which still covers client-side navigation - would never run
// and a first visit got a blank browser error page where the standalone app used to show the
// styled 404. Re-execute on the shared /not-found page, keeping the 404 status code (the
// standalone app could only answer 200, since index.html was its fallback for everything).
app.UseStatusCodePagesWithReExecute("/not-found");

app.MapStaticAssets();
app.UseAntiforgery();
app.UseRateLimiter();

// Built once at startup from the same catalog the nav panel and the MCP server read, so a page
// added there is advertised to search engines without a second list to remember. The URLs are
// absolute and point at production (SiteMetadata.Origin) rather than at the serving host: a
// preview deployment that advertised its own address would compete with the real site for the
// same content. Pages carrying a "noindex" meta are left out - PageOutlet reads that same list.
var siteMapUrls = DocsCatalog.AllPages
    .Select(page => page.Url)
    .Where(url => SiteMetadata.NoIndexUrls.Contains(url) is false)
    .Select(url => $"<url><loc>{SiteMetadata.AbsoluteUrl(url)}</loc></url>");

var siteMap = $"""
    <?xml version="1.0" encoding="UTF-8"?>
    <urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">
    {string.Join(Environment.NewLine, siteMapUrls)}
    </urlset>
    """;

app.MapGet("/sitemap.xml", () => Results.Text(siteMap, "application/xml", Encoding.UTF8));

// Both are declared before the Razor components below: /api/... and /mcp are literal routes that
// no page owns, but keeping them first says out loud that they are not part of the app's UI.
app.MapControllers().RequireRateLimiting(McpRateLimiterPolicy);
app.MapMcp("/mcp").RequireRateLimiting(McpRateLimiterPolicy);

app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Bit.Bswup.Demo.Client._Imports).Assembly);

app.Run();
