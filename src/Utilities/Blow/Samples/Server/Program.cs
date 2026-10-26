using Bit.Blow.Samples.Core;
using Bit.Blow.Samples.Server.Components;

var builder = WebApplication.CreateBuilder(args);

// The pages run here, on the server, and only their markup reaches the browser. Every feature of the canvas works,
// but every pointer move of a drag, a pan or a connection is a Blazor event that crosses the circuit and comes back as
// a render: a few milliseconds on a local network, enough for a dragged node to trail the pointer over the internet.
// The layout, validation and serialization the pages call are plain C#, and run here too.
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
