﻿using System;
using System.Net.Http;
using System.Reflection;
using Microsoft.AspNetCore.Components.WebView.Maui;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Hosting;

namespace Bit.Client.Web.BlazorUI.Playground.Web
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();

            builder
                .RegisterBlazorMauiWebView()
                .UseMauiApp<App>()
                .Host.ConfigureAppConfiguration((app, config) =>
                {
                    var assembly = typeof(MauiProgram).GetTypeInfo().Assembly;
                    config.AddJsonFile(new EmbeddedFileProvider(assembly), "appsettings.json", optional: false, false);
                });

            var services = builder.Services;

            services.AddBlazorWebView();
            services.AddPlaygroundServices();
            services.AddSingleton(scope => new HttpClient { BaseAddress = new Uri(scope.GetService<IConfiguration>().GetValue<string>("ApiServerAddress")) });

            return builder.Build();
        }
    }
}
