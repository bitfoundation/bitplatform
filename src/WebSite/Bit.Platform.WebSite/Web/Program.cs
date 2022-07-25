using System.Threading.Tasks;
#if BlazorWebAssembly
using System;
using System.Net.Http;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
#elif BlazorServer
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
#endif

namespace Bit.Platform.WebSite.Web;

public class Program
{
    public static async Task Main(string[] args)
    {
        await CreateHostBuilder(args)
            .RunAsync();
    }

#if BlazorWebAssembly
    public static WebAssemblyHost CreateHostBuilder(string[] args)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault(args);
        builder.Services.AddSingleton(new HttpClient { BaseAddress = new Uri($"{builder.HostEnvironment.BaseAddress}api/") });

        builder.Services.AddPlaygroundServices();

        return builder.Build();
    }
#elif BlazorServer
    public static WebApplication CreateHostBuilder(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

#if DEBUG
        builder.WebHost.UseUrls("https://*:4001", "http://*:4000");
#endif

        Startup.Services.Add(builder.Services, builder.Configuration);

        var app = builder.Build();

        Startup.Middlewares.Use(app, builder.Environment);

        return app;
    }
#endif
}
