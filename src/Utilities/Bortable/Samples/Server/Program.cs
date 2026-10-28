using Bit.Bortable.Samples.Core;
using Bit.Bortable.Samples.Server.Components;

var builder = WebApplication.CreateBuilder(args);

// The pages run here, on the server, and only their markup reaches the browser. Every feature of the lists works: the
// browser follows and measures the pointer itself, and the circuit carries only what changes - the start of a drag,
// each new drop position, and its end. A drop position trails the pointer by the network's latency, and the rules a
// list asks (CanDrop, MaxItems, ConfirmDrop) run here, next to the data they decide on.
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
