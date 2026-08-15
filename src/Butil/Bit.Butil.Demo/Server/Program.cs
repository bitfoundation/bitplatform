using Bit.Butil.Demo.Client.Docs;
using Bit.Butil.Demo.Server.Components;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

// The prerender pass instantiates the client's components in this container, so it has to
// register the very same services the WebAssembly container does.
builder.Services.AddDemoServices();

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
// is to hand an assistant the map of the site without making it scrape 68 pages of chrome to
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
        // The client closed the stream (or navigated away) - the normal way this ends.
    }
});

app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Bit.Butil.Demo.Client._Imports).Assembly);

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
