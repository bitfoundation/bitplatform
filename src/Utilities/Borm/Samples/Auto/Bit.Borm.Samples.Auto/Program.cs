using Bit.Borm.Samples.Auto.Components;
using Bit.Borm.Samples.Core;

var builder = WebApplication.CreateBuilder(args);

// InteractiveAuto: the first visit runs on a circuit while the WebAssembly runtime downloads, later visits run in the
// browser. Borm works on both, so a form validates from the first visit on - over the circuit at first, then in the
// browser without a round trip.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

// The pages run here while prerendering, for the static form's post and while the first visit is on Interactive
// Server, so they need the same services the WebAssembly client registers.
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

app.UseHttpsRedirection();
// The static form sample posts with an antiforgery token, which this checks.
app.UseAntiforgery();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Routes).Assembly);

app.Run();
