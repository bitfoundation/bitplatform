using Bit.BevTools;
using Bit.BevTools.Samples.Lazy.Client;
using Bit.BevTools.Samples.Lazy.Components;

var builder = WebApplication.CreateBuilder(args);

// This host prerenders the pages; they run on WebAssembly in the browser, which downloads Bit.BevTools.wasm only when
// someone opens the dev tools. There is no circuit, so the server package is not needed.
builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

// The layout injects BevToolsLoader, and is prerendered here too, so the server registers the loader as the client
// does. A process that serves many users does not open BevTools outside Development (no AllowInMultiUserHost and
// Authorize here), so the prerendered page offers no "Open dev tools" button; the browser's copy of the page does.
builder.Services.AddBitBevToolsLoaderServices(o => o.RegisterAtStartup = builder.Environment.IsDevelopment());

var app = builder.Build();

app.UseAntiforgery();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Routes).Assembly);

app.Run();
