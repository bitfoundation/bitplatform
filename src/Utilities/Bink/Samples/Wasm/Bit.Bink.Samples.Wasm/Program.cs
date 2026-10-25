using Bit.Bink.Samples.Core;
using Bit.Bink.Samples.Wasm.Components;

var builder = WebApplication.CreateBuilder(args);

// This host only prerenders the pages; they run on WebAssembly in the browser, where BinkCanvas hands its command
// buffer to JavaScript without serializing it (BinkCanvas.UsesFastRenderPath).
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
