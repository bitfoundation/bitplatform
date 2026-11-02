using Bit.Balette;
using Bit.Balette.Samples.Lazy.Client;
using Bit.Balette.Samples.Lazy.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Services;

var builder = WebApplication.CreateBuilder(args);

// This host prerenders the pages; they run on WebAssembly in the browser, which downloads Bit.Balette.wasm only when a
// page with the palette opens.
builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

// The server loads Balette with the rest of the app and prerenders the admin page, its palette and trigger included, so
// it registers Balette's services as any host does. Only the client, which cannot, goes without them.
builder.Services.AddBitBaletteServices();

// Routes.razor injects it, and is prerendered here as well. Outside the browser it downloads nothing.
builder.Services.AddScoped<LazyAssemblyLoader>();

var app = builder.Build();

app.UseAntiforgery();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Routes).Assembly);

app.Run();
