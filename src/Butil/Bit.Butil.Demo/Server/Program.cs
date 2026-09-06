using System.Threading.RateLimiting;
using Bit.Butil.Demo.Client.Docs;
using ModelContextProtocol.Protocol;
using Bit.Butil.Demo.Server.Components;
using Bit.Butil.Demo.Server.Controllers;
using Bit.Butil.Demo.Server.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.RateLimiting;

// The CORS policy the two MCP routes opt into, defined here and named on the controller so the
// GET mirror carries it as endpoint metadata rather than inheriting it from MapControllers().
const string McpCorsPolicy = McpController.CorsPolicy;

// The name the streaming demos' concurrency limit and their deadline are both registered under.
const string StreamingPolicy = "streaming";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

// The prerender pass instantiates the client's components in this container, so it has to
// register the very same services the WebAssembly container does.
builder.Services.AddDemoServices();

// The MCP server (Controllers/McpController.cs) and the plain HTTP endpoints that mirror it.
builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();
builder.Services.AddMcpServer(options =>
{
    options.ServerInfo = new Implementation
    {
        Name = "bit-butil",
        Title = "Bit.Butil - the browser platform for Blazor",
        Version = ButilApiCatalog.Version,
        WebsiteUrl = "https://github.com/bitfoundation/bitplatform/tree/develop/src/Butil"
    };

    // The one field a server gets to write directly into the model's context, once, before it has
    // called anything. Everything here is what the tool descriptions cannot say individually: which
    // tool to reach for first, and the four facts about this library that turn compiling code into
    // working code. It is deliberately short - it is paid for on every request of every session.
    options.ServerInstructions = ButilMcpInstructions.Text;
})
    .WithHttpTransport()
    .WithToolsFromAssembly()
    .WithResourcesFromAssembly()
    .WithPromptsFromAssembly()
    // Argument autocompletion for the prompts and the resource templates. Their arguments are all
    // drawn from closed sets this server already holds - the hosting models, the docs slugs, the
    // type names - and without this a person picking a prompt in their editor is asked to type one
    // with nothing to type it from. See Services/ButilCompletions.cs.
    .WithCompleteHandler((context, _) => ValueTask.FromResult(ButilCompletions.Complete(context.Params)));

// Browser-based MCP clients - and the "connect a server" flows built into web-hosted agents - call
// /mcp with fetch from another origin, and a browser will not hand them the response unless the
// server says so. Everything behind these two routes is public read-only documentation served
// anonymously, so there is nothing here that an origin check was protecting: without this the
// endpoint is simply unreachable from a browser, which is where a growing share of MCP clients run.
// AllowAnyOrigin and credentials are mutually exclusive by design, and that is the right way round -
// no cookie of this site's should ever ride along on a cross-origin tool call.
builder.Services.AddCors(options => options.AddPolicy(McpCorsPolicy, policy => policy
    .AllowAnyOrigin()
    .AllowAnyHeader()
    .WithMethods("GET", "POST", "DELETE", "OPTIONS")
    // A cross-origin caller cannot read a response header that is not named here. The negotiated
    // protocol revision is the one this transport still answers with - Mcp-Session-Id is not, because
    // streamable HTTP is stateless by default now that SEP-2567 has removed sessions from it.
    .WithExposedHeaders("MCP-Protocol-Version", "WWW-Authenticate")));

// Renders a docs page outside of a request's component hierarchy, so its content can be handed to
// an MCP client as text. Scoped: a renderer belongs to the request that asked for the page.
builder.Services.AddScoped<HtmlRenderer>();

// The three streaming demos below hold a connection open on purpose - that is what they are
// demonstrating - which on a public site is also how a handful of clients can hold every thread
// this app has. Two limits make that bounded rather than open-ended: how many such requests one
// client may have at once, and how long any of them may last. Both are the framework's own
// middleware, so the endpoints keep answering their own cancellation token and nothing else.
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy(StreamingPolicy, context => RateLimitPartition.GetConcurrencyLimiter(
        // The connection's address, not a header a caller writes: X-Forwarded-For is the caller's
        // to invent, and a partition key anyone can change is not a limit.
        partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        // Eight at once, queueing none: the pages themselves open at most a handful (the shared-signal
        // demo alone starts three), so this is well clear of what the site does and far short of what
        // holding the connections is worth to anyone else. A refusal is immediate rather than queued,
        // because a queued slot on a five-minute stream is a connection held for the same reason.
        factory: _ => new ConcurrencyLimiterOptions { PermitLimit = 8, QueueLimit = 0 }));

    // A rejected client is told to come back rather than left to guess, and 429 is the answer a
    // fetch() or an EventSource can act on.
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

// A stream that is never read still ends: the token these handlers already watch is cancelled at
// the deadline, so they finish the way they do when the client goes away.
builder.Services.AddRequestTimeouts(options =>
    options.AddPolicy(StreamingPolicy, new RequestTimeoutPolicy { Timeout = TimeSpan.FromMinutes(5) }));

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

// Before the endpoints, so the preflight OPTIONS a cross-origin MCP client sends is answered by
// the middleware rather than falling through to a route that does not handle it.
app.UseCors();

app.UseAntiforgery();

// Both only act on the endpoints that ask for them by name - the streaming demos below.
app.UseRateLimiter();
app.UseRequestTimeouts();

// For the WebSocket page's echo endpoint below. Off by default in ASP.NET Core, and the upgrade
// handshake happens in middleware, so this has to be in the pipeline before the endpoint runs.
app.UseWebSockets();

app.MapStaticAssets();

// The MCP server, and the same tools as plain HTTP GETs under /api/mcp/... so each of them is
// inspectable from a browser. Both are literal routes, so they never compete with the app's pages.
// The GET mirror opts into the policy with [EnableCors] on McpController itself, so a controller
// added to this app later does not silently inherit an open one.
app.MapControllers();
app.MapMcp("/mcp").RequireCors(McpCorsPolicy);

// Discovery files - for crawlers and, increasingly, for the AI assistants people ask about this
// library instead of searching. All three are generated from DocsNav rather than written by hand,
// so a page added to the nav is a page that appears here; a checked-in copy would silently rot.
// The origin comes from the request, so there is no deployment URL to configure or get wrong.
app.MapGet("/robots.txt", (HttpContext context) =>
    Results.Text($"""
        User-agent: *
        Allow: /

        Sitemap: {Origin(context)}/sitemap.xml

        """, "text/plain"));

app.MapGet("/sitemap.xml", (HttpContext context) =>
{
    var origin = Origin(context);
    var urls = string.Concat(SitePages().Select(p =>
        $"""
          <url><loc>{origin}{(p.Url.Length == 0 ? "/" : $"/{p.Url}")}</loc><priority>{p.Priority}</priority></url>

        """));

    return Results.Text($"""
        <?xml version="1.0" encoding="UTF-8"?>
        <urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">
        {urls}</urlset>
        """, "application/xml");
});

// https://llmstxt.org - an H1, a blockquote summary, then H2-delimited lists of links. The point
// is to hand an assistant the map of the site without making it scrape 97 pages of chrome to
// rebuild one.
app.MapGet("/llms.txt", (HttpContext context) =>
{
    var origin = Origin(context);
    var sections = string.Concat(DocsNav.Groups.Select(g =>
        $"""

        ## {g.Title}

        {string.Concat(g.Links.Select(l => $"- [{l.Title}]({origin}/{l.Url}): {l.Summary}\n"))}
        """));

    return Results.Text($"""
        # Bit.Butil

        > Bit.Butil wraps the browser platform - window, document, storage, crypto, media, sensors,
        > workers - as injectable, strongly-typed, XML-documented C# services for Blazor, so a Blazor
        > app can call the Web APIs without writing JavaScript or hand-rolling IJSRuntime interop.
        > It works in Blazor WebAssembly, Server and Hybrid, and under prerendering, on .NET 8, 9 and 10.

        Install with `dotnet add package Bit.Butil`, add `<script src="_content/Bit.Butil/bit-butil.js">`
        to the host page before the Blazor script, and call `builder.Services.AddBitButilServices()`.
        Every wrapper is a scoped service you inject by its own name, e.g. `@inject Bit.Butil.Clipboard clipboard`.

        Each page below documents one browser API: what it wraps, runnable samples, an API reference
        table, and the preconditions it imposes (HTTPS, a permission prompt, a user gesture).
        {sections}
        ## Optional

        - [MCP endpoint]({origin}/mcp): this same site as tools an AI agent can call over streamable HTTP - search,
          the exact API of every service, what each one needs from the page, and the setup per hosting model. What it
          exposes is documented at {origin}/mcp-server, listed above; every tool is also a plain HTTP GET under
          `{origin}/api/mcp/...`, which is the quickest way to see what one answers.
        - [NuGet package](https://www.nuget.org/packages/Bit.Butil): the published package.
        - [Source repository](https://github.com/bitfoundation/bitplatform): issues and source.

        """, "text/markdown");
});

// A live text/event-stream endpoint so the EventSource page has something real to connect to.
// It ticks once a second, alternates between an unnamed and a named event so both listener kinds
// are exercised, and stamps an id so a reconnect can be seen resuming from Last-Event-ID.
app.MapGet("/sse/ticks", async (HttpContext context, CancellationToken cancellationToken) =>
{
    context.Response.Headers.ContentType = "text/event-stream";
    context.Response.Headers.CacheControl = "no-cache";
    // Proxies that buffer would defeat the whole point; this is the conventional opt-out.
    context.Response.Headers["X-Accel-Buffering"] = "no";

    var next = int.TryParse(context.Request.Headers["Last-Event-ID"], out var lastId) ? lastId + 1 : 1;

    try
    {
        while (cancellationToken.IsCancellationRequested is false)
        {
            var payload = $$"""{"tick":{{next}},"at":"{{DateTimeOffset.UtcNow:HH:mm:ss}}"}""";
            var name = next % 3 == 0 ? "heartbeat" : null;

            await context.Response.WriteAsync($"id: {next}\n", cancellationToken);
            if (name is not null) await context.Response.WriteAsync($"event: {name}\n", cancellationToken);
            await context.Response.WriteAsync($"data: {payload}\n\n", cancellationToken);
            await context.Response.Body.FlushAsync(cancellationToken);

            next++;
            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
        }
    }
    catch (OperationCanceledException)
    {
        // The client closed the stream (or navigated away), or the deadline came - the normal ways
        // this ends.
    }
})
.RequireRateLimiting(StreamingPolicy)
.WithRequestTimeout(StreamingPolicy);

// A real socket for the WebSocket page to talk to. The handler lives in WebSocketEcho because the
// E2E suite hosts the same one on a loopback port and asserts on this protocol - see that file.
app.MapGet("/ws/echo", Bit.Butil.Demo.Server.Endpoints.WebSocketEcho.Handle);

// Something worth streaming, for the Streams page: a body that arrives in visible instalments
// rather than all at once, with a Content-Length so progress has a denominator. The pause between
// chunks is what makes "read it as it arrives" observably different from "wait, then read it".
app.MapGet("/api/stream", async (HttpContext context, CancellationToken cancellationToken, int chunks = 20, int chunkSize = 4096, int delayMs = 100) =>
{
    var count = Math.Clamp(chunks, 1, 200);
    var size = Math.Clamp(chunkSize, 1, 64 * 1024);
    // Clamped like the other two, and for a sharper reason: Task.Delay(-1) waits for ever.
    var delay = Math.Clamp(delayMs, 0, 5_000);

    context.Response.Headers.ContentType = "application/octet-stream";
    context.Response.Headers.CacheControl = "no-store";
    context.Response.Headers["X-Accel-Buffering"] = "no";
    context.Response.ContentLength = (long)count * size;

    // Repeating text rather than random bytes: it compresses, which is what makes the page's
    // "pipe it through the browser's gzip codec" section show a number worth looking at.
    var chunk = System.Text.Encoding.UTF8.GetBytes(new string('x', size));

    try
    {
        for (var i = 0; i < count && cancellationToken.IsCancellationRequested is false; i++)
        {
            await context.Response.Body.WriteAsync(chunk, cancellationToken);
            await context.Response.Body.FlushAsync(cancellationToken);
            await Task.Delay(delay, cancellationToken);
        }
    }
    catch (OperationCanceledException)
    {
        // The reader cancelled - which is one of the things the page demonstrates.
    }
})
.RequireRateLimiting(StreamingPolicy)
.WithRequestTimeout(StreamingPolicy);

// A request that takes its time, so the AbortController page has something real to cancel. It
// streams a byte a second rather than sleeping and then answering: a response that has not started
// can be aborted by anything, while one already streaming proves the abort reaches the transfer.
app.MapGet("/api/slow", async (HttpContext context, CancellationToken cancellationToken, int seconds = 10) =>
{
    context.Response.Headers.ContentType = "text/plain";
    context.Response.Headers.CacheControl = "no-store";
    context.Response.Headers["X-Accel-Buffering"] = "no";

    try
    {
        for (var second = 0; second < Math.Clamp(seconds, 1, 60); second++)
        {
            await context.Response.WriteAsync($"{second}\n", cancellationToken);
            await context.Response.Body.FlushAsync(cancellationToken);
            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
        }
    }
    catch (OperationCanceledException)
    {
        // The client aborted - which is what the page is demonstrating, not a failure.
    }
})
.RequireRateLimiting(StreamingPolicy)
.WithRequestTimeout(StreamingPolicy);

app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Bit.Butil.Demo.Client._Imports).Assembly);

// SearchButil is the tool an agent reaches for first, and its index is the most expensive thing
// here to build - a reflection walk over the whole library plus every catalog. Built in the
// background from startup, no caller waits for it; the index stays lazy, so nothing is delayed and
// a build that fails leaves the site up and is retried by the first caller rather than swallowed.
_ = Task.Run(ButilSearchIndex.Warm).ContinueWith(
    task => app.Logger.LogError(task.Exception, "Building the Bit.Butil search index failed at startup. SearchButil will rebuild it on the next call."),
    TaskContinuationOptions.OnlyOnFaulted);

// The site's own search corpus (Services/DocsContentIndex.cs), on the same terms: it parses every
// page on the site, so the first visitor to open the search box should not be the one who pays for
// that. Also lazy, so a failure here costs the search box its content index and nothing else.
_ = Task.Run(DocsContentIndex.Warm).ContinueWith(
    task => app.Logger.LogError(task.Exception, "Building the docs search index failed at startup. The site's search box will rebuild it on the next request."),
    TaskContinuationOptions.OnlyOnFaulted);

app.Run();

// The absolute origin this request arrived on. Behind a reverse proxy the forwarded-headers
// middleware has already rewritten Scheme and Host by the time an endpoint runs, so this is the
// public origin rather than the container's.
static string Origin(HttpContext context) => $"{context.Request.Scheme}://{context.Request.Host}";

// Every crawlable route, in reading order, with the home page first. The guide pages outrank the
// per-API reference the way they do in the nav: someone arriving from a search engine wants
// "getting started" before "VisualViewport".
static IEnumerable<(string Url, string Priority)> SitePages() =>
    [("", "1.0"), .. DocsNav.AllLinks.Select(l => (l.Url, l.Support is ApiSupport.Guide ? "0.9" : "0.8"))];
