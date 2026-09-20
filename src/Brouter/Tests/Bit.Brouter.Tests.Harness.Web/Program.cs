using Bit.Brouter.Tests.Harness.Web.Components;

var builder = WebApplication.CreateBuilder(args);

// Both interactive runtimes are always registered: which one (if any) the harness uses is decided by
// App.razor from BrouterHarness:Mode.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddBrouterHarness();

var app = builder.Build();

#if NET9_0_OR_GREATER
app.UseAntiforgery();
app.MapStaticAssets();
#else
app.UseStaticFiles();
// Explicitly after the static files: WebApplication otherwise matches endpoints first, the catch-all
// page claims /_content/... and UseStaticFiles steps aside for a request that already has an endpoint.
app.UseRouting();
app.UseAntiforgery();
#endif

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode();

app.Run();
