using Bit.Bonboarding;
using Bit.Bonboarding.Samples.Lazy.Client;
using Bit.Bonboarding.Samples.Lazy.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Services;

var builder = WebApplication.CreateBuilder(args);

// This host prerenders the pages; they run on WebAssembly in the browser, which downloads Bit.Bonboarding.wasm only
// when an admin page, the one part of the app with a tour, opens.
builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

// The server loads Bonboarding with the rest of the app and prerenders the admin pages - their tour and BonboardingRoot
// included - so it registers Bonboarding's services as any host does. Only the client, which cannot, goes without them.
builder.Services.AddBitBonboardingServices();

// Routes.razor injects it, and is prerendered here as well. Outside the browser it downloads nothing.
builder.Services.AddScoped<LazyAssemblyLoader>();

var app = builder.Build();

app.UseAntiforgery();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Routes).Assembly);

app.Run();
