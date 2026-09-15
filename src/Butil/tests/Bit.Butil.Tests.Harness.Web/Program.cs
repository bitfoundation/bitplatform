using Bit.Butil;
using Bit.Butil.Samples.Core;
using ButilTests.Harness.Web;
using ButilTests.Harness.Web.Components;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

// Both interactive runtimes are always registered: which one (if any) the pages use is decided by App.razor
// from ButilHarness:Mode.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

// The prerender pass and the Server circuits instantiate the pages in this container, so it registers exactly
// what the WebAssembly client's does.
builder.Services.AddCoreServices();

// Process-wide, like the real toggle: every circuit this host serves loads its modules lazily. The WebAssembly
// client reads the same decision off the page (see its Program.cs).
if (HarnessScripts.IsLazy(builder.Configuration))
{
    BitButil.UseLazyScripts();
}

var app = builder.Build();

// The worker scripts, frame and stream sample the harness pages load by URL (see the csproj for where they come
// from). Ahead of everything else, and from the application's own folder, so the same files answer in a build
// output, a publish output and an in-process WebApplicationFactory alike.
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(Path.Combine(AppContext.BaseDirectory, "harness-files"))
});

#if NET9_0_OR_GREATER
app.UseAntiforgery();
app.MapStaticAssets();
#else
app.UseStaticFiles();
// Explicitly after the static files: WebApplication otherwise matches endpoints first, a page route claims
// the request and UseStaticFiles steps aside for a request that already has an endpoint.
app.UseRouting();
app.UseAntiforgery();
#endif

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Routes).Assembly);

app.Run();
