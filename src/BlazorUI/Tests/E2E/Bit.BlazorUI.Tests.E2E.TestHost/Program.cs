using Bit.BlazorUI;
using Bit.BlazorUI.Tests.E2E.TestHost;

var builder = WebApplication.CreateBuilder(args);

// Every error the host logs is kept for the tests to read (/api/server-errors): a component that throws while it is
// disposed, or a circuit an unhandled exception tears down, shows nothing on the page itself.
builder.Logging.AddProvider(new ServerErrorLog());
// Which pages each circuit was on, so a test reads the errors of its own pages alone (?paths=...).
builder.Services.AddScoped<Microsoft.AspNetCore.Components.Server.Circuits.CircuitHandler, CircuitPaths>();

// The services the components read - the theme and accent color switchers, the page visibility the carousels pause
// on - and the HttpClient BitFileUpload uploads with, registered the way an app registers them.
builder.Services.AddBitBlazorUIServices();
builder.Services.AddBitBlazorUIExtrasServices();
builder.Services.AddScoped(_ => new HttpClient());

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

if (app.Environment.IsDevelopment() is false)
{
    app.UseExceptionHandler("/Error");
}

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<Bit.BlazorUI.Tests.E2E.TestHost.Components.App>()
    .AddInteractiveServerRenderMode();

// What BitFileUpload sends its files to: one endpoint that takes every chunk, one that refuses every chunk (how an
// upload fails), and one that answers slowly (how an upload is seen in progress and cancelled).
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
app.MapPost("/api/upload-slow", async (HttpRequest request, CancellationToken cancellationToken) =>
{
    await request.Body.CopyToAsync(Stream.Null, cancellationToken);
    await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
    return Results.Ok();
});
app.MapDelete("/api/upload", () => Results.Ok());

// The errors the host logged, numbered so a test can ask for the ones logged since it started (?after=N), and
// narrowed to the circuits that were on the pages it visited (?paths=/e2e/a,/e2e/b).
app.MapGet("/api/server-errors", (int? after, string? paths) =>
    Results.Ok(ServerErrorLog.Since(after ?? 0, paths?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))));
app.MapDelete("/api/server-errors", () =>
{
    ServerErrorLog.Clear();
    return Results.Ok();
});

app.Run();

public partial class Program { }
