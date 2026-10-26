using Bit.Borm.Samples.Core;
using Bit.Borm.Samples.Server.Components;
using Microsoft.AspNetCore.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

// The pages run here, on the server: every value a field writes crosses the circuit, and the rules, conditions and
// computed values run here a round trip later. That is why the fields update on change by default, and why the builder
// sample's username debounces its keystrokes.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Borm's renderer registry, in every container a sample page is created in. Borm calls no browser API, so
// prerendering, and the static form's post, are safe.
builder.Services.AddCoreServices();

var app = builder.Build();

if (app.Environment.IsDevelopment() is false)
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
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(typeof(Routes).Assembly);

app.Run();
