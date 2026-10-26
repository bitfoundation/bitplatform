using Bit.Bmotion.Samples.Server.Components;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// The pages run here, at the other end of a circuit. Bmotion hands what it animates to the browser
// over async interop, so nearly everything plays as it does on WebAssembly; only what needs the
// per-frame loop lands on its target at once (see BmotionCapabilities.SupportsFrameLoop).
builder.Services.AddCoreServices();

var app = builder.Build();

if (app.Environment.IsDevelopment() is false)
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseAntiforgery();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(typeof(Bit.Bmotion.Samples.Core._Imports).Assembly);

app.Run();
