using Bit.Bmage;
using Bit.Bmage.Samples.Lazy.Client;
using Bit.Bmage.Samples.Lazy.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Services;

var builder = WebApplication.CreateBuilder(args);

// This host prerenders the pages; they run on WebAssembly in the browser, which downloads Bit.Bmage.wasm and
// Bit.Bmage.Blazor.wasm only when the editor's page opens.
builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

// The server loads Bmage with the rest of the app and prerenders the editor, so it registers Bmage's services as the
// guide asks of a server that prerenders. Only the client, which cannot, goes without them.
builder.Services.AddBitBmageServices();

// Routes.razor injects it, and is prerendered here as well. Outside the browser it downloads nothing.
builder.Services.AddScoped<LazyAssemblyLoader>();

var app = builder.Build();

app.UseAntiforgery();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Routes).Assembly);

app.Run();
