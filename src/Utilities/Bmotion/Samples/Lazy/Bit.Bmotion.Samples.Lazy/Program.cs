using Bit.Bmotion;
using Bit.Bmotion.Samples.Lazy.Client;
using Bit.Bmotion.Samples.Lazy.Client.Layout;
using Bit.Bmotion.Samples.Lazy.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Services;

var builder = WebApplication.CreateBuilder(args);

// This host prerenders the pages; they run on WebAssembly in the browser, which downloads Bit.Bmotion.wasm only when the
// showcase, the part of the app that animates, opens.
builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

// The server loads Bmotion with the rest of the app and prerenders the showcase, so it registers Bmotion's services as
// any host does - with the options the showcase's layout gives ConfigureBmotionServices in the browser, from the same
// method. While it prerenders that layout, the layout's call checks that the two agree: the browser takes the elements
// prerendered at rest over at rest only when they do. Only the client, which cannot register them, goes without.
builder.Services.AddBitBmotionServices(ShowcaseLayout.ConfigureBmotion);

// Routes.razor injects it, and is prerendered here as well. Outside the browser it downloads nothing.
builder.Services.AddScoped<LazyAssemblyLoader>();

var app = builder.Build();

app.UseAntiforgery();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Routes).Assembly);

app.Run();
