// Blazor Auto: a page runs here, in a circuit, until the browser has the WebAssembly runtime, and in the browser from
// then on (see Bit.Bcrdt.Samples.Auto.Client). Both kinds of replica connect through the hub below, so a tab on Server
// rendering and a tab on WebAssembly edit the same document.

using Bit.Bcrdt.Samples.Auto.Components;
using Bit.Bcrdt.Samples.Core;
using Bit.Bcrdt.Server;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http.Features;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

// The client's registration but for the hub's address, which a circuit takes from this server rather than from the
// browser (see SamplesRelay): whichever side renders interactively owns the replica. Both keep their copy in the
// browser's IndexedDB - the circuit's through JS interop - so the two meet in the same store too.
builder.Services.AddCoreServices(collaboration => collaboration
    .UseSamplesRelay(services => SamplesRelay.LocalHubAddress(
        services.GetRequiredService<IServer>().Features.GetRequiredFeature<IServerAddressesFeature>().Addresses))
    .UseIndexedDbStore());

// A circuit outlives its tab by the circuit retention period; this withdraws a closed tab from everybody's presence
// list as soon as its connection drops instead.
builder.Services.AddBitBcrdtCircuitPresence();

// The relay every replica meets through, and a document host behind it: one more replica of every document, so a
// document outlives every tab that had it open. Its store is in memory here; an application gives it a durable one.
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

// Pages move to HTTPS; the hub is left where it is asked for. A browser reaches it at its HTTPS page's own address
// anyway, and its other clients - the circuits' replicas, a MAUI app on an emulator - reach it over plain HTTP without
// having to trust the development certificate.
app.UseWhen(context => context.Request.Path.StartsWithSegments("/hub/bcrdt") is false, https => https.UseHttpsRedirection());
app.UseAntiforgery();

app.MapStaticAssets();

// Picks the registered document host up by itself.
app.MapHub<BcrdtHub>("/hub/bcrdt");

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Bit.Bcrdt.Samples.Core._Imports).Assembly);

app.Run();
