using Bit.Brouter.Demo.Server.Components;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

// The prerender pass instantiates the client's components in this container, so it has to
// register the very same services the WebAssembly container does.
builder.Services.AddDemoServices();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();

app.MapStaticAssets();

// Components/Pages/Host.razor is a catch-all page, so every deep link matches an endpoint and
// gets the prerendered app back - Brouter then resolves the real route (including its own 404).
app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode();

app.Run();
