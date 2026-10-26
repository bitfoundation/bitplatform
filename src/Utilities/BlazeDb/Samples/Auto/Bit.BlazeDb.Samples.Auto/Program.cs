using Bit.BlazeDb.Samples.Core.Data;
using Bit.BlazeDb.Samples.Auto.Components;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

// The pages run here while prerendering and through the first visit, on Interactive Server while the WebAssembly
// runtime downloads, so they need a database service here too - one that opens nothing: the database lives in the
// browser tab, and there is no OPFS on a server. The pages render a placeholder until they run on WebAssembly.
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
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Bit.BlazeDb.Samples.Core._Imports).Assembly);

app.Run();
