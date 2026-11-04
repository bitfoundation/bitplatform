using Bit.Bink.Samples.Lazy.Client;
using Bit.Bink.Samples.Lazy.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Services;

var builder = WebApplication.CreateBuilder(args);

// This host prerenders the pages; they run on WebAssembly in the browser, which downloads Bit.Bink.wasm only when a
// page that draws opens.
builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

// Routes.razor injects it, and is prerendered here as well. Outside the browser it downloads nothing. There is nothing
// of Bink's to register: the server loads Bink with the rest of the app, and the canvas takes only IJSRuntime and
// NavigationManager, which every host registers.
builder.Services.AddScoped<LazyAssemblyLoader>();

var app = builder.Build();

app.UseAntiforgery();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Routes).Assembly);

app.Run();
