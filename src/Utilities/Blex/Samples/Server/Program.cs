using Bit.Blex.Samples.Core;
using Bit.Blex.Samples.Server.Components;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// One DI container: the prerender pass and every circuit (one DI scope each) render the pages here.
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
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(typeof(Routes).Assembly);

app.Run();
