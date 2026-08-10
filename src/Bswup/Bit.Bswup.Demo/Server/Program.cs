using System.IO.Compression;
using Bit.Bswup.Demo.Server.Components;
using Microsoft.AspNetCore.ResponseCompression;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

// App.razor reads the "no-prerender" query off HttpContext to honor the service worker's
// escape hatch (see self.noPrerenderQuery in the client's service-worker files).
builder.Services.AddHttpContextAccessor();

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

app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Bit.Bswup.Demo.Client._Imports).Assembly);

app.Run();
