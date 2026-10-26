// Blazor Auto: a page runs here, in a circuit, until the browser has the WebAssembly runtime, and in the browser from
// then on (see Bit.Bonboarding.Samples.Auto.Client). Both sides keep progress in the same localStorage, so a tour left
// part way on the circuit resumes at the same step once the page runs on WebAssembly.

using Bit.Bonboarding.Samples.Auto.Components;
using Bit.Bonboarding.Samples.Core;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

// The client's registration: whichever side renders a page interactively runs its tours.
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

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

app.UseHttpsRedirection();
app.UseAntiforgery();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Routes).Assembly);

app.Run();
