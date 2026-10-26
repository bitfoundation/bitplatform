// Blazor WebAssembly in a Blazor Web App: this server only prerenders the sample pages, and they run in the browser
// from then on (see Bit.Bonboarding.Samples.Wasm.Client).

using Bit.Bonboarding.Samples.Core;
using Bit.Bonboarding.Samples.Wasm.Components;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

// The client's registration, so the pages get the same services while they are prerendered here. No tour runs here:
// AutoStart and OnAfterRenderAsync only fire once a page is interactive, in the browser.
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
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Routes).Assembly);

app.Run();
