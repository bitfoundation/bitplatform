using Bit.BbScanner.Samples.Core;
using Bit.BbScanner.Samples.Server.Components;

var builder = WebApplication.CreateBuilder(args);

// The pages run here, on the server, and only their markup reaches the browser. BbScanner reads the camera through the
// WebAssembly runtime, which this host does not have, so the scanner reports NotSupported - saying which renderer it
// is on - instead of offering a Start button that does nothing. BbBarcodeDecoder runs here in-process (the decoder
// page), which is how a Server app decodes images it has turned into pixels.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

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
    .AddAdditionalAssemblies(typeof(Routes).Assembly);

app.Run();
