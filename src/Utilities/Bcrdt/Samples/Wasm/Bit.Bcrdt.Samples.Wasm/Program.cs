// Blazor WebAssembly in a Blazor Web App: this server prerenders the sample pages and relays between the replicas,
// and every replica is a document in a browser tab's own memory (see Bit.Bcrdt.Samples.Wasm.Client).

using Bit.Bcrdt.Samples.Wasm.Components;
using Bit.Bcrdt.Server;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

// The same registration as the client, so the pages get the same services while they are prerendered here. Nothing
// is opened here: BcrdtCollaborationScope opens its document only once the page runs in the browser.
builder.Services.AddCoreServices(collaboration => collaboration
    .UseSamplesRelay()
    .UseIndexedDbStore());

// The relay every tab meets through, and a document host behind it: one more replica of every document, so a document
// outlives every tab that had it open. Its store is in memory here; an application gives it a durable one.
builder.Services.AddBitBcrdtHub();
builder.Services.AddBitBcrdtDocumentHost();

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

// Picks the registered document host up by itself.
app.MapHub<BcrdtHub>("/hub/bcrdt");

app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Bit.Bcrdt.Samples.Core._Imports).Assembly);

app.Run();
