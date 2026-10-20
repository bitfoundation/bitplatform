using Bit.Blex.Tests.Harness;
using Bit.Blex.Tests.Harness.Web.Components;

var builder = WebApplication.CreateBuilder(args);

// Both interactive runtimes are always registered: which one (if any) the harness uses is decided by
// App.razor from BlexHarness:Mode.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

// The prerender pass and the Server circuits render the harness in this container; the WebAssembly
// client registers the very same services in its own.
builder.Services.AddBlexHarness();

var app = builder.Build();

#if NET9_0_OR_GREATER
app.UseAntiforgery();
app.MapStaticAssets();
#else
app.UseStaticFiles();
app.UseRouting();
app.UseAntiforgery();
#endif

// The harness pages live in the harness library, so its assembly is what gives every route an endpoint.
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(HarnessApp).Assembly);

app.Run();

/// <summary>Named so an in-process test can host this exact file.</summary>
public partial class Program;
