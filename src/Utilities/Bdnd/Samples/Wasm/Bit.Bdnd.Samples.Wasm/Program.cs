using Bit.Bdnd.Samples.Core;
using Bit.Bdnd.Samples.Wasm.Components;

var builder = WebApplication.CreateBuilder(args);

// This host only prerenders the pages; they run on WebAssembly in the browser, the best fit for Bdnd: every dragover
// and hover change is handled without a network round trip, and a custom drag layer is aligned from the first frame.
builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

// The prerender pass creates the same pages on the server, so they need the same services here.
builder.Services.AddCoreServices();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
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
