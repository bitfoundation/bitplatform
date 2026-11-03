using Bit.Bsqlite.Samples.Lazy.Client;
using Bit.Bsqlite.Samples.Lazy.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Services;

var builder = WebApplication.CreateBuilder(args);

// This host prerenders the pages; they run on WebAssembly in the browser, which downloads Bit.Bsqlite.wasm, EF Core and
// the library with the app's model only when the products page opens.
builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

// The server loads Bsqlite with the rest of the app, so it registers the services as usual - with the method the browser
// builds them from, as the guide says a server that prerenders the pages does. Only the client, which cannot, goes
// without. Nothing opens a database here: the products page asks for one only once it runs in the browser.
AppDatabase.Register(builder.Services);

// Routes.razor injects it, and is prerendered here as well. Outside the browser it downloads nothing.
builder.Services.AddScoped<LazyAssemblyLoader>();

var app = builder.Build();

app.UseAntiforgery();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Routes).Assembly);

app.Run();
