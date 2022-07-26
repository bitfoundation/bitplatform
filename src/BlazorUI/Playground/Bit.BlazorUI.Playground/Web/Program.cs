﻿using System.Threading.Tasks;
#if BlazorWebAssembly
using System;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
#elif BlazorServer
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
#endif

namespace Bit.BlazorUI.Playground.Web;

public class Program
{
    public static async Task Main(string[] args)
    {
#if BlazorWebAssembly || BlazorServer
        await CreateHostBuilder(args).RunAsync();
#else
        System.Console.WriteLine("You're in blazor hybrid mode, please run app project instead of web project.");
#endif
    }

#if BlazorWebAssembly
    public static WebAssemblyHost CreateHostBuilder(string[] args)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault(args);
        builder.Services.AddSingleton(new HttpClient { BaseAddress = new Uri($"{builder.HostEnvironment.BaseAddress}api/") });
        builder.Services.AddScoped<Microsoft.AspNetCore.Components.WebAssembly.Services.LazyAssemblyLoader>();

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
