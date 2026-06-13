var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<Bit.BlazorUI.Tests.Performance.TestHost.Components.App>()
    .AddInteractiveServerRenderMode();

// Endpoint used by performance tests to force a server-side GC and report heap size.
// This allows tests to verify that server memory returns to near-baseline after
// disposing components, confirming that leaks (e.g. undisposed DotNetObjectReference
// or unsubscribed event handlers) have been resolved.
//
// Two collections are performed - the first queues finalizers, the second reclaims
// them - so the reported figure converges on the steady-state heap and isn't skewed
// by recently-finalized objects.
app.MapGet("/api/gc-info", () =>
{
    GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true);
    GC.WaitForPendingFinalizers();
    GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true);
    var allocatedMB = GC.GetTotalMemory(forceFullCollection: false) / (1024.0 * 1024.0);
    return Results.Ok(new { allocatedMB });
});

app.Run();

// Make Program class accessible for WebApplicationFactory
public partial class Program { }
