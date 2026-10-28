using Bit.Bortable.Samples.Wasm.Components;
using Bit.Bortable.Samples.Core;

var builder = WebApplication.CreateBuilder(args);

// This host only prerenders the pages; they run on WebAssembly in the browser, where every new drop position is
// decided in the same process as the UI, with no network hop. A prerendered list shows its items in their order at
// once, and takes a drag once WebAssembly takes over.
builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

// Here as in the client project: the prerendered pages inject the same drag coordinator, so the server's container
// needs it too, or the first prerendered list throws.
builder.Services.AddCoreServices();

var app = builder.Build();

if (app.Environment.IsDevelopment() is false)
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

// A deep link to an unknown address matches no endpoint, which would end in an empty 404: re-execute it to render the
// shared Not found page instead, keeping the status code.
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

app.UseHttpsRedirection();
app.UseAntiforgery();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Routes).Assembly);

app.Run();
