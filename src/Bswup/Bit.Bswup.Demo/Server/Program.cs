using System.IO.Compression;
using Bit.Bswup.Demo.Server.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.ResponseCompression;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

// App.razor reads the "no-prerender" query off HttpContext to honor the service worker's
// escape hatch (see self.noPrerenderQuery in the client's service-worker files).
builder.Services.AddHttpContextAccessor();

// The MCP server (Controllers/McpController.cs) and the plain HTTP endpoints that mirror it.
builder.Services.AddControllers();
builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithToolsFromAssembly()
    .WithResourcesFromAssembly()
    .WithPromptsFromAssembly();

// Renders a docs page outside of a request's component hierarchy, so its content can be handed to
// an MCP client as text. Scoped: a renderer belongs to the request that asked for the page.
builder.Services.AddScoped<HtmlRenderer>();

// The MCP explorer page (Client/Pages/McpPage.razor) injects an HttpClient. It only ever calls one
// after the browser has taken over, but the component is also prerendered here - and rendered here
// again when an MCP client asks for that page - so the dependency has to resolve in this container
// too, or both of those fail with nothing rendered.
builder.Services.AddScoped(_ => new HttpClient());

builder.Services.AddResponseCompression(opts =>
{
    opts.EnableForHttps = true;
    opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(["application/octet-stream"]);
    opts.Providers.Add<BrotliCompressionProvider>();
    opts.Providers.Add<GzipCompressionProvider>();
})
    .Configure<BrotliCompressionProviderOptions>(opt => opt.Level = CompressionLevel.Fastest)
    .Configure<GzipCompressionProviderOptions>(opt => opt.Level = CompressionLevel.Fastest);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
    app.UseResponseCompression();
}

app.UseHttpsRedirection();

// A deep link that matches no page (/typo, and likewise a missing static file) never reaches
// Blazor: routing answers it with an empty 404 before MapRazorComponents is consulted, so the
// router's <NotFound> fragment - which still covers client-side navigation - would never run
// and a first visit got a blank browser error page where the standalone app used to show the
// styled 404. Re-execute on the shared /not-found page, keeping the 404 status code (the
// standalone app could only answer 200, since index.html was its fallback for everything).
app.UseStatusCodePagesWithReExecute("/not-found");

app.MapStaticAssets();
app.UseAntiforgery();

// Both are declared before the Razor components below: /api/... and /mcp are literal routes that
// no page owns, but keeping them first says out loud that they are not part of the app's UI.
app.MapControllers();
app.MapMcp("/mcp");

app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Bit.Bswup.Demo.Client._Imports).Assembly);

app.Run();
