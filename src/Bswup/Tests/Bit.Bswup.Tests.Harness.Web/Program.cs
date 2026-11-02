using Bit.Bswup.Tests.Harness.Web;
using Bit.Bswup.Tests.Harness.Web.Components;

var builder = WebApplication.CreateBuilder(args);

// Both interactive runtimes are always registered: which one the harness uses is decided by App.razor
// from BswupHarness:Mode.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddSingleton<HarnessSessions>();

var app = builder.Build();

// Fail at startup on an unknown mode: a typo must not let a whole suite pass against another mode.
HarnessRenderModes.Resolve(HarnessRenderModes.FromConfiguration(app.Configuration), prerender: true);

// First, so the control endpoints, the simulated network conditions and the per-session worker script
// apply to every request - static assets included.
app.UseMiddleware<HarnessMiddleware>();

// Explicit, and after the middleware: WebApplication would otherwise match endpoints before it runs, and
// the middleware rewrites /standalone/_content/ request paths.
app.UseRouting();
app.UseAntiforgery();
app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode();

app.Run();
