#if BlazorWebAssembly
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http;
#elif BlazorServer
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
#endif
using System;
using System.Threading.Tasks;

namespace Bit.Client.Web.BlazorUI.Playground.Web
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
#if BlazorWebAssembly || BlazorServer
            await CreateHostBuilder(args)
                .Build()
                .RunAsync();
#else
            Console.WriteLine("You're in blazor hybrid mode, please run app project isntead of web project.");
#endif
        }

#if BlazorWebAssembly
        public static WebAssemblyHostBuilder CreateHostBuilder(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault();

            builder.RootComponents.Add<App>("app");

            builder.Services.AddSingleton(new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

            new Startup().ConfigureServices(builder.Services);

            return builder;
        }
#elif BlazorServer
        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
#endif
    }
}
