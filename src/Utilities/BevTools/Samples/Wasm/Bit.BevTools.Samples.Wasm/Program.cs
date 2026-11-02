using Bit.BevTools;
using Bit.BevTools.Samples.Core;
using Bit.BevTools.Samples.Wasm.Components;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddCoreServices();

// The server only prerenders here, and <BevToolsPanel /> is prerendered with the rest of the page: it renders nothing
// then, but records what prerendering did and hands it to the WebAssembly session (Runtime panel). There is no
// circuit, so the server package is not needed.
builder.Services.AddBitBevToolsServices();

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
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Routes).Assembly);

app.Run();
