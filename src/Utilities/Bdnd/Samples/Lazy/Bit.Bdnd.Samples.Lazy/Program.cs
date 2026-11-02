using Bit.Bdnd;
using Bit.Bdnd.Samples.Lazy.Client;
using Bit.Bdnd.Samples.Lazy.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Services;

var builder = WebApplication.CreateBuilder(args);

// This host prerenders the pages; they run on WebAssembly in the browser, which downloads Bit.Bdnd.wasm only when a page
// that drags opens.
builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

// The server loads Bdnd with the rest of the app and prerenders the board, so it registers Bdnd's services as any host
// does. Only the client, which cannot, goes without them.
builder.Services.AddBitBdndServices();

// Routes.razor injects it, and is prerendered here as well. Outside the browser it downloads nothing.
builder.Services.AddScoped<LazyAssemblyLoader>();

var app = builder.Build();

app.UseAntiforgery();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Routes).Assembly);

app.Run();
