using Bit.Bink.Samples.Core;
using Bit.Bink.Samples.Server.Components;

var builder = WebApplication.CreateBuilder(args);

// The engine runs here and the canvas in the browser: pointer samples travel over the circuit once per animation
// frame and canvas commands travel back. Nothing has to be configured for it - not even the circuit's maximum message
// size, because whatever the browser sends back (exported images, pasted content) arrives as a stream.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

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

app.UseHttpsRedirection();
app.UseAntiforgery();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(typeof(Routes).Assembly);

app.Run();
