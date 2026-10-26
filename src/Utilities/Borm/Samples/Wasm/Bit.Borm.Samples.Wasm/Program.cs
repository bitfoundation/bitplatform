using Bit.Borm.Samples.Core;
using Bit.Borm.Samples.Wasm.Components;
using Microsoft.AspNetCore.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

// This host only prerenders the pages; they run on WebAssembly in the browser, the best fit for Borm: every rule,
// condition and computed value runs on each value a field writes, and none of them waits on a network round trip.
builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

// The prerender pass, and the static form's post, create the same pages on the server, so they need the same services
// here.
builder.Services.AddCoreServices();

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

// A deep link to an unknown address matches no endpoint, which would end in an empty 404: re-execute it to render the
// shared Not found page instead, keeping the status code.
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
// Only a 404, though: any other empty error - the 400 a form post with a stale antiforgery token gets, say - is not a
// missing address, and the Not found page would tell the user it was.
app.Use(async (context, next) =>
{
    await next();
    if (context.Response.StatusCode != StatusCodes.Status404NotFound
        && context.Features.Get<IStatusCodePagesFeature>() is { } statusCodePages)
    {
        statusCodePages.Enabled = false;
    }
});

app.UseHttpsRedirection();
// The static form sample posts with an antiforgery token, which this checks.
app.UseAntiforgery();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Routes).Assembly);

app.Run();
