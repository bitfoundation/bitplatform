using Bit.Bdnd.Samples.Core;
using Bit.Bdnd.Samples.Server.Components;

var builder = WebApplication.CreateBuilder(args);

// The pages run here, on the server, and every drag goes through the circuit: the browser module reports the native
// events, the callbacks run here a round trip later, and dropped files stream over SignalR. A custom drag layer starts
// from an estimated position that is corrected once the measured one arrives.
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
