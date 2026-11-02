using Bit.BevTools;
using Bit.BevTools.Samples.Core;
using Bit.BevTools.Samples.Server.Components;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCoreServices();

// BevTools and its circuit instrumentation. It turns itself on in Development only; a process that serves many users
// stays off elsewhere unless told otherwise (see the Security section of the README).
builder.Services.AddBitBevToolsServerServices();

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

// What the Network page requests.
app.MapGet("/api/weather", () => new { summary = "Sunny", temperatureC = 23 });

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(typeof(Routes).Assembly);

app.Run();
