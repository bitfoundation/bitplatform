using Bit.Brouter.NewDemo.Server.Components;

var builder = WebApplication.CreateBuilder(args);

// Blazor Web App with interactive server components. Prerendering is on by default for the
// InteractiveServer render mode, which is exactly what this demo is meant to showcase: the
// Brouter now matches the current URL during the server prerender pass (in OnInitializedAsync)
// instead of waiting for OnAfterRenderAsync, so the matched route's markup is present in the
// initial HTML response before the SignalR circuit connects.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Registers IBrouter / BrouterService and the Brouter options (via the shared Core extension).
builder.Services.AddCoreServices();

var app = builder.Build();

if (app.Environment.IsDevelopment() is false)
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAntiforgery();

// Registers the framework + project static web assets (blazor.web.js, _content/* css, etc.) as
// ENDPOINTS. This is essential here: Host.razor uses a catch-all "/{*path}" page route so Brouter
// can match any URL, and a catch-all endpoint would otherwise swallow requests for
// /_framework/blazor.web.js (returning HTML instead of JS, which kills hydration). MapStaticAssets
// gives those assets dedicated, higher-precedence endpoints so they win over the catch-all.
app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
