using System;
using System.Net.Http;
using System.Reflection;
using Microsoft.AspNetCore.Components.WebView.Maui;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Hosting;

namespace Bit.BlazorUI.Playground.Web
{
    public static class MauiProgram
    {
        public static MauiAppBuilder CreateMauiAppBuilder()
        {
#if !BlazorHybrid
        throw new InvalidOperationException("Please switch to blazor hybrid as described in readme.md");
#endif

            var builder = MauiApp.CreateBuilder();

            var assembly = typeof(MauiProgram).GetTypeInfo().Assembly;

            builder
                .UseMauiApp<App>()
                .Configuration.AddJsonFile(new EmbeddedFileProvider(assembly), "appsettings.json", optional: false, false);

            var services = builder.Services;

            services.AddMauiBlazorWebView();
#if DEBUG
            services.AddBlazorWebViewDeveloperTools();
#endif

            services.AddPlaygroundServices();

            return builder;
        }
    }
}
