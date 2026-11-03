using Bit.Bcrdt;
using Bit.Bcrdt.Samples.Lazy.Client;
using Bit.Bcrdt.Samples.Lazy.Components;
using Bit.Bcrdt.Server;
using Microsoft.AspNetCore.Components.WebAssembly.Services;

var builder = WebApplication.CreateBuilder(args);

// This host prerenders the pages and relays between the tabs; the pages run on WebAssembly in the browser, which
// downloads Bit.Bcrdt.wasm only when a page that collaborates opens.
builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

// The server loads Bcrdt with the rest of the app and prerenders the notes, so it registers Bcrdt's services as any host
// does - and the notes' layout gets this registration back from GetBcrdtCollaborationService. It needs neither a
// transport nor a store: BcrdtCollaborationScope opens its document only once the page runs in the browser. Only the
// client, which cannot register them, goes without.
builder.Services.AddBitBcrdtServices();

// Routes.razor injects it, and is prerendered here as well. Outside the browser it downloads nothing.
builder.Services.AddScoped<LazyAssemblyLoader>();

var app = builder.Build();

app.UseWebSockets();
app.UseAntiforgery();

app.MapStaticAssets();

// The relay the tabs meet through. Its client, the WebSocket transport, is part of Bit.Bcrdt, so the notes need no other
// assembly downloaded with it. It only relays: a document lives in the tabs that have it open, and in their IndexedDB.
app.MapBitBcrdtWebSocket("/ws/bcrdt/{documentId}");

app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Routes).Assembly);

app.Run();
