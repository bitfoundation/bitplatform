using Bit.BlazeDb.Samples.Core.Data;
using Bit.BlazeDb.Samples.Wasm.Components;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

// The prerender pass creates the same pages on the server, so they need a database service here too - one that
// opens nothing: the database lives in the browser tab, and there is no OPFS on a server. The pages render a
// placeholder until they run on WebAssembly.
builder.Services.AddCoreServices<UnreachableSampleDatabase>();

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

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseAntiforgery();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Bit.BlazeDb.Samples.Core._Imports).Assembly);

app.Run();
