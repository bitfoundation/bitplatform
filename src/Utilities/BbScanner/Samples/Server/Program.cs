using Bit.BbScanner.Samples.Core;
using Bit.BbScanner.Samples.Server.Components;

var builder = WebApplication.CreateBuilder(args);

// The pages run here, on the server, and only their markup reaches the browser. BbScanner opens the camera in the
// browser, which cuts each frame down to the luminance of the searched region and streams it over the circuit; the
// frame loop and the decoder run here. The page closes a camera the circuit leaves behind. BbBarcodeDecoder runs here
// in-process too (the decoder page).
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
