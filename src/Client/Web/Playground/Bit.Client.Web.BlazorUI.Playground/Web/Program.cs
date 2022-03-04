using System;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
#if BlazorWebAssembly
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
#elif BlazorServer
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
#endif
using Microsoft.Extensions.DependencyInjection;

#if BlazorWebAssembly
        
var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddSingleton(new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddPlaygroundServices();

var app = builder.Build();
await app.RunAsync();

#elif BlazorServer

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddPlaygroundServices();

builder.Services.AddHttpClient("ApiHttpClient", (serviceProvider, httpClient) =>
{
    httpClient.BaseAddress = new Uri(serviceProvider.GetRequiredService<IConfiguration>()["ApiServerAddress"]);
});
builder.Services.AddTransient(c => c.GetRequiredService<IHttpClientFactory>().CreateClient("ApiHttpClient"));
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddResponseCompression(opts =>
    {
        opts.EnableForHttps = true;
        opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Where(m => m != "text/html").Concat(new[] { "application/octet-stream" }).ToArray();
        opts.Providers.Add<BrotliCompressionProvider>();
        opts.Providers.Add<GzipCompressionProvider>();
    })
    .Configure<BrotliCompressionProviderOptions>(opt => opt.Level = CompressionLevel.Fastest)
    .Configure<GzipCompressionProviderOptions>(opt => opt.Level = CompressionLevel.Fastest);

var app = builder.Build();

if (builder.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
}

app.UseHttpsRedirection();
app.UseResponseCompression();
app.UseStaticFiles();

app.UseRouting();

app.UseEndpoints(endpoints =>
{
    endpoints.MapDefaultControllerRoute();
    endpoints.MapBlazorHub();
    endpoints.MapFallbackToPage("/_Host");
});

await app.RunAsync();

#else

Console.WriteLine("You're in blazor hybrid mode, please run app project isntead of web project.");

#endif
