using Bit.Blow.Samples.Auto.Components;
using Bit.Blow.Samples.Core;

var builder = WebApplication.CreateBuilder(args);

// InteractiveAuto: the first visit runs on a circuit while the WebAssembly runtime downloads, later visits run in the
// browser. The canvas works on both and looks the same; what changes is where a drag is handled - on the server for
// the first visit, then in the browser once the runtime is cached. The renderer each page names says which one it got.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

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
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Routes).Assembly);

app.Run();
