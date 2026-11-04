using Bit.Blow.Samples.Lazy.Client;
using Bit.Blow.Samples.Lazy.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Services;

var builder = WebApplication.CreateBuilder(args);

// This host prerenders the pages; they run on WebAssembly in the browser, which downloads Bit.Blow.wasm only when the
// page with a canvas opens.
builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

// Routes.razor injects it, and is prerendered here as well. Outside the browser it downloads nothing. There is nothing
// of Blow's to register: the server loads Blow with the rest of the app, and the canvas takes only IJSRuntime, which
// every host registers.
builder.Services.AddScoped<LazyAssemblyLoader>();

var app = builder.Build();

app.UseAntiforgery();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Routes).Assembly);

app.Run();
