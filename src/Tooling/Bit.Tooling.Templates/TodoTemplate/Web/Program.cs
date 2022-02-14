﻿#if BlazorServer
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
#elif BlazorWebAssembly
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
#endif

namespace TodoTemplate.App;

public class Program
{
    public static async Task Main(string[] args)
    {
#if BlazorWebAssembly || BlazorServer
        await CreateHostBuilder(args)
            .RunAsync();
#else
            Console.WriteLine("You're in blazor hybrid mode, please run app project isntead of web project.");
#endif
    }

#if BlazorWebAssembly
    public static WebAssemblyHost CreateHostBuilder(string[] args)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault();

        builder.Services.AddSingleton(sp => new HttpClient(sp.GetRequiredService<TodoTemplateHttpClientHandler>()) { BaseAddress = new Uri($"{builder.HostEnvironment.BaseAddress}api/") });
        builder.Services.AddTransient<ITokenProvider, ClientSideTokenProvider>();

        builder.Services.AddTodoTemplateSharedServices();
        builder.Services.AddTodoTemplateAppServices();

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
