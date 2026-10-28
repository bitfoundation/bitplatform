using Bit.Blex.Samples.Core;
using Bit.Blex.Samples.Wasm.Components;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

// The prerender pass renders the very same pages the browser then hydrates, so this container
// registers exactly what the WebAssembly client's does - through the same extension method.
builder.Services.AddWebServices();

var app = builder.Build();

if (app.Environment.IsDevelopment() is false)
{
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found");
app.UseHttpsRedirection();
app.UseAntiforgery();

app.MapStaticAssets();

// The pages live in the Core library, so its assembly is what gives every route an endpoint.
app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Routes).Assembly);

app.Run();
