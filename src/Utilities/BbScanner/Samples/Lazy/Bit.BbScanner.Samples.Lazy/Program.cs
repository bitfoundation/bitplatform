using Bit.BbScanner.Samples.Lazy.Client;
using Bit.BbScanner.Samples.Lazy.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Services;

var builder = WebApplication.CreateBuilder(args);

// This host prerenders the pages; they run on WebAssembly in the browser, which downloads Bit.BbScanner.wasm only when
// the page with the scanner opens. BbScanner has no services, so there is nothing of it to register here either: the
// server loads it with the rest of the app and prerenders the scanner's markup, which stays inert until the browser
// takes over.
builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

// Routes.razor injects it, and is prerendered here as well. Outside the browser it downloads nothing.
builder.Services.AddScoped<LazyAssemblyLoader>();

var app = builder.Build();

app.UseAntiforgery();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Routes).Assembly);

app.Run();
