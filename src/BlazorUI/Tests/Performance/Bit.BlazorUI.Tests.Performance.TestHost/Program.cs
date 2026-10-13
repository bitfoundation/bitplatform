using Bit.BlazorUI;

var builder = WebApplication.CreateBuilder(args);

// The errors the host logs are kept for the browser tests to read (/api/server-errors): a component that throws while
// it is disposed, or a circuit an unhandled exception tears down, shows nothing on the page.
builder.Logging.AddProvider(new Bit.BlazorUI.Tests.Performance.TestHost.ServerErrorLog());

// Add services to the container.
// The services the components read of the library's own - the theme and the accent color switchers, the page
// visibility the carousels pause on - and the HttpClient BitFileUpload uploads with.
builder.Services.AddBitBlazorUIExtrasServices();
builder.Services.AddScoped(_ => new HttpClient());

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// The services some components inject (BitCarousel's page visibility, ...), as an app registers them.
builder.Services.AddBitBlazorUIServices();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<Bit.BlazorUI.Tests.Performance.TestHost.Components.App>()
    .AddInteractiveServerRenderMode();

// Endpoint used by performance tests to force a server-side GC and report heap size.
// This allows tests to verify that server memory returns to near-baseline after
// disposing components, confirming that leaks (e.g. undisposed DotNetObjectReference
// or unsubscribed event handlers) have been resolved.
//
// Two collections are performed - the first queues finalizers, the second reclaims
// them - so the reported figure converges on the steady-state heap and isn't skewed
// by recently-finalized objects.
app.MapGet("/api/gc-info", () =>
{
    GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true);
    GC.WaitForPendingFinalizers();
    GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true);
    var allocatedMB = GC.GetTotalMemory(forceFullCollection: false) / (1024.0 * 1024.0);
    return Results.Ok(new { allocatedMB });
});

// A host page with nothing but the theme setup (BitThemePairBrowserTests): the
// root attributes the server resolves from this request's cookies, the first-paint script, and the library
// script that self-initializes from them - no Blazor, so a reload is a plain first paint. The query states the
// page's setup: default, light, dark, persist=1, cookie=1, available (comma-separated).
app.MapGet("/regression/theme-host", (HttpContext context) =>
{
    var query = context.Request.Query;
    var options = new BitThemeSsrOptions
    {
        DefaultTheme = query["default"],
        LightTheme = query["light"],
        DarkTheme = query["dark"],
        Persist = query["persist"] == "1",
        PersistCookie = query["cookie"] == "1",
        PersistedThemePair = context.Request.Cookies[BitThemeCookie.PairCookieName],
        AvailableThemes = ((string?)query["available"])?.Split(','),
    };
    var attributes = BitThemeSsr.BuildRootThemeAttributes(context.Request.Cookies[BitThemeCookie.PreferenceCookieName], options);

    return Results.Content($"""
        <!DOCTYPE html>
        <html lang="en" {attributes}>
        <head>
            <meta charset="utf-8" />
            {BitThemeSsr.InlineHeadScript}
        </head>
        <body>
            <script src="/_content/Bit.BlazorUI/scripts/bit.blazorui.js"></script>
        </body>
        </html>
        """, "text/html");
});

// What BitFileUpload sends its files to (/regression/form-inputs): one endpoint that takes every chunk, and one that
// refuses every chunk, which is how an upload fails.
app.MapPost("/api/upload", async (HttpRequest request) =>
{
    await request.Body.CopyToAsync(Stream.Null);
    return Results.Ok();
});
app.MapPost("/api/upload-fail", async (HttpRequest request) =>
{
    await request.Body.CopyToAsync(Stream.Null);
    return Results.StatusCode(StatusCodes.Status500InternalServerError);
});

app.MapGet("/api/server-errors",() => Results.Ok(Bit.BlazorUI.Tests.Performance.TestHost.ServerErrorLog.Entries.ToArray()));
app.MapDelete("/api/server-errors", () =>
{
    Bit.BlazorUI.Tests.Performance.TestHost.ServerErrorLog.Entries.Clear();
    return Results.Ok();
});

app.Run();

// Make Program class accessible for WebApplicationFactory
public partial class Program { }
