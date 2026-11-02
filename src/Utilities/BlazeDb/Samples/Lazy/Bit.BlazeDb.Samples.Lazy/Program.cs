using Bit.BlazeDb.Samples.Lazy.Client;
using Bit.BlazeDb.Samples.Lazy.Client.Data;
using Bit.BlazeDb.Samples.Lazy.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Services;

var builder = WebApplication.CreateBuilder(args);

// This host prerenders the pages; they run on WebAssembly in the browser, which downloads Bit.BlazeDb.wasm only when the
// todos page opens.
builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

// The prerender creates the todos page here too, and it injects AppDatabase. Nothing opens it here: the database lives
// in the browser tab, and the page asks for it only once it runs there. BlazeDb itself has nothing to register.
builder.Services.AddSingleton<AppDatabase>();

// Routes.razor injects it, and is prerendered here as well. Outside the browser it downloads nothing.
builder.Services.AddScoped<LazyAssemblyLoader>();

var app = builder.Build();

app.UseAntiforgery();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Routes).Assembly);

app.Run();
