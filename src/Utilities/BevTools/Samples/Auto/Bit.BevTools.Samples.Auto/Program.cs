using Bit.BevTools;
using Bit.BevTools.Samples.Core;
using Bit.BevTools.Samples.Auto.Components;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddCoreServices();

// Until Auto switches a visit to WebAssembly, the pages run on a circuit here. The client's Program.cs registers
// BevTools for the WebAssembly side, so it works whichever runtime a page ends up in.
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
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Routes).Assembly);

app.Run();
