// Blazor Server: every sample page runs here, in the user's circuit. The Bonboarding services are scoped and a scope is
// a circuit, so each tab has a tour engine of its own; the library's module measures and observes in the browser, and
// progress is read from and written to that browser's localStorage over the circuit.

using Bit.Bonboarding.Samples.Core;
using Bit.Bonboarding.Samples.Server.Components;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCoreServices();

var app = builder.Build();

if (app.Environment.IsDevelopment() is false)
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
    .AddAdditionalAssemblies(typeof(Routes).Assembly);

app.Run();
