using Bit.Belemetry.Tests.Harness;
using Bit.Belemetry.Tests.Harness.Web;
using Bit.Belemetry.Tests.Harness.Web.Collector;
using Bit.Belemetry.Tests.Harness.Web.Components;

var builder = WebApplication.CreateBuilder(args);

// Both interactive runtimes are always registered: which one (if any) the harness uses is decided by App.razor from
// BelemetryHarness:Mode.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

// The exporters post to this very host (the capturing receiver below), so the browser's exports are same-origin and
// need no CORS.
builder.Services.AddBelemetryHarness(
    HarnessConfiguration.CollectorOrigin(builder.Configuration),
    builder.Configuration.GetValue<bool>(HarnessConfiguration.ServerPipelineKey));

builder.Services.AddSingleton<HarnessCollector>();

// The echo endpoint doubles as a cross-origin API (the page calls it under the host's other name), so it has to
// allow the traceparent header the way a real API behind another origin must.
builder.Services.AddCors(options => options.AddPolicy(HarnessCollectorEndpoints.CrossOriginPolicy,
    policy => policy.AllowAnyOrigin().WithMethods("GET").WithHeaders("traceparent", "tracestate")));

var app = builder.Build();

#if NET9_0_OR_GREATER
app.UseCors();
app.UseAntiforgery();
app.MapStaticAssets();
#else
app.UseStaticFiles();
app.UseRouting();
app.UseCors();
app.UseAntiforgery();
#endif

app.MapHarnessCollector();

// The pages live in the harness library; their routes become endpoints through the additional assembly, and every
// one renders App, whose HarnessApp routes to the page.
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(HarnessApp).Assembly);

app.Run();
