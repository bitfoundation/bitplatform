using Bit.Blow.Samples.Wasm.Components;
using Bit.Blow.Samples.Core;

var builder = WebApplication.CreateBuilder(args);

// This host only prerenders the pages; they run on WebAssembly in the browser, the model Blow recommends for editing:
// every pointer move is handled in the same process as the UI, with no network hop. A prerendered canvas shows its
// markup at once, and measures its nodes and handles once WebAssembly takes over.
builder.Services.AddRazorComponents()
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
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Routes).Assembly);

app.Run();
