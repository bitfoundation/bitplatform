using Bit.Bmotion;
using BmotionTests.Harness.Web.Client;
using BmotionTests.Harness.Web.Components;

var builder = WebApplication.CreateBuilder(args);

// Both interactive runtimes are always registered: which one (if any) a page uses is decided by App.razor, from
// BmotionHarness:Mode or the request's ?mode=.
builder.Services.AddRazorComponents()
    // Detailed errors: a circuit an exception terminated is what a failing Server test sees, and the browser
    // console line that reports it is the one place the actual cause can be quoted from.
    .AddInteractiveServerComponents(o => o.DetailedErrors = true)
    .AddInteractiveWebAssemblyComponents();

// The prerender pass and the Server circuits instantiate the pages in this container, so it registers the library
// exactly as the WebAssembly client does - with the options App.razor hands the client on <body data-bm-*>.
var options = HarnessOptions.Read(key => builder.Configuration[key]);
builder.Services.AddSingleton(options);
builder.Services.AddBitBmotionServices(options.Apply);

var app = builder.Build();

#if NET9_0_OR_GREATER
app.UseAntiforgery();
app.MapStaticAssets();
#else
app.UseStaticFiles();
// Explicitly after the static files: WebApplication otherwise matches endpoints first, a page route claims the
// request and UseStaticFiles steps aside for a request that already has an endpoint.
app.UseRouting();
app.UseAntiforgery();
#endif

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Routes).Assembly);

app.Run();
