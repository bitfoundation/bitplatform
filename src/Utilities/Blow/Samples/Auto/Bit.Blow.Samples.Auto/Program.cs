using Bit.Blow.Samples.Auto.Components;
using Bit.Blow.Samples.Core;

var builder = WebApplication.CreateBuilder(args);

// InteractiveAuto: the first visit runs on a circuit while the WebAssembly runtime downloads, later visits run in the
// browser. The canvas works on both and looks the same; what changes is where a drag is handled - on the server for
// the first visit, then in the browser once the runtime is cached. The renderer each page names says which one it got.
// On the circuit everything the browser sends arrives as one SignalR message, and the default limit of 32 KB is
// smaller than a saved document of a hundred-odd nodes coming back from the Saving page's text box: past the limit the
// circuit is closed. 1 MB holds thousands of nodes. Keep a limit: every byte of it is memory a client can make the
// server buffer.
builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents()
    .AddInteractiveServerComponents()
    .AddHubOptions(options => options.MaximumReceiveMessageSize = 1024 * 1024);

builder.Services.AddCoreServices();

var app = builder.Build();

if (app.Environment.IsDevelopment() is false)
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
