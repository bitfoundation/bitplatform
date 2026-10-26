// Blazor Server: every sample page runs here, in the user's circuit. The collaboration service is scoped and a scope
// is a circuit, so each tab gets a replica - and a client identity - of its own, which reaches the others through this
// app's own hub exactly as a browser would.

using Bit.Bcrdt.Samples.Core;
using Bit.Bcrdt.Samples.Server.Components;
using Bit.Bcrdt.Server;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http.Features;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// The replicas connect through the hub below, at an address this server listens on rather than the one the browser
// reported (see SamplesRelay), and keep their copy of each document in their user's browser, through the circuit's JS
// interop - so a document comes back after a reload even before the hub answers.
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

if (app.Environment.IsDevelopment() is false)
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
    .AddAdditionalAssemblies(typeof(Bit.Bcrdt.Samples.Core._Imports).Assembly);

app.Run();
