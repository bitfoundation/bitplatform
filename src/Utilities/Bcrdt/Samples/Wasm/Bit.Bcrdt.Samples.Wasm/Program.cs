// Blazor WebAssembly in a Blazor Web App: this server prerenders the sample pages and relays between the replicas,
// and every replica is a document in a browser tab's own memory (see Bit.Bcrdt.Samples.Wasm.Client).

using Bit.Bcrdt.Samples.Core;
using Bit.Bcrdt.Samples.Wasm.Components;
using Bit.Bcrdt.Server;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http.Features;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

// The client's registration, so the pages get the same services while they are prerendered here. Nothing is opened
// here: BcrdtCollaborationScope opens its document only once the page runs in the browser. Should a replica ever run
// here, it reaches the hub at an address this server listens on, never at one the browser reported (see SamplesRelay).
builder.Services.AddCoreServices(collaboration => collaboration
    .UseSamplesRelay(services => SamplesRelay.LocalHubAddress(
        services.GetRequiredService<IServer>().Features.GetRequiredFeature<IServerAddressesFeature>().Addresses))
    .UseIndexedDbStore());

// The relay every tab meets through, and a document host behind it: one more replica of every document, so a document
// outlives every tab that had it open. Its store is in memory here; an application gives it a durable one.
builder.Services.AddBitBcrdtHub();
builder.Services.AddBitBcrdtDocumentHost();

var app = builder.Build();

if (app.Environment.IsDevelopment() is false)
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

// Pages move to HTTPS; the hub is left where it is asked for. A browser reaches it at its HTTPS page's own address
// anyway, and its other clients - a MAUI app on an emulator, say - reach it over plain HTTP without having to trust the
// development certificate.
app.UseWhen(context => context.Request.Path.StartsWithSegments("/hub/bcrdt") is false, https => https.UseHttpsRedirection());
app.UseAntiforgery();

app.MapStaticAssets();

// Picks the registered document host up by itself.
app.MapHub<BcrdtHub>("/hub/bcrdt");

app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Bit.Bcrdt.Samples.Core._Imports).Assembly);

app.Run();
