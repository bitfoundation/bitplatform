using BlazorEmpty.Components;
using Microsoft.AspNetCore.ResponseCompression;
#if (UseWebAssembly)
using BlazorEmpty.Client.Pages;
#endif

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment() is false)
{
    builder.Services.AddResponseCompression(opts =>
        opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(["application/wasm", "application/octet-stream"]));
}

// Add services to the container.
#if (!UseServer && !UseWebAssembly)
builder.Services.AddRazorComponents();
#else
builder.Services.AddRazorComponents()
#if (UseServer && UseWebAssembly)
                .AddInteractiveServerComponents()
                .AddInteractiveWebAssemblyComponents();
#elif (UseServer)
                .AddInteractiveServerComponents();
#elif (UseWebAssembly)
                .AddInteractiveWebAssemblyComponents();
#endif
#endif

builder.Services.AddBitBlazorUIServices();

var app = builder.Build();

// Configure the HTTP request pipeline.
#if (UseWebAssembly)
if (builder.Environment.IsDevelopment())
{
    builder.UseWebAssemblyDebugging();
}
else
#else
if (builder.Environment.IsDevelopment() is false)
#endif
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

if (builder.Environment.IsDevelopment() is false)
{
    app.UseHttpsRedirection();
    app.UseResponseCompression();
}

app.UseStaticFiles();
app.UseAntiforgery();

app.MapStaticAssets();
#if (UseServer && UseWebAssembly)
app.MapRazorComponents<App>()
   .AddInteractiveServerRenderMode()
   .AddInteractiveWebAssemblyRenderMode()
#elif (UseServer)
app.MapRazorComponents<App>()
   .AddInteractiveServerRenderMode();
#elif (UseWebAssembly)
app.MapRazorComponents<App>()
   .AddInteractiveWebAssemblyRenderMode()
#else
app.MapRazorComponents<App>();
#endif
#if (UseWebAssembly)
   .AddAdditionalAssemblies(typeof(BlazorEmpty.Client._Imports).Assembly);
#endif

app.Run();
