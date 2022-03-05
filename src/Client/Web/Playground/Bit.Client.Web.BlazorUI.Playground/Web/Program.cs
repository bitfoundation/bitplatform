using System.Threading.Tasks;
#if BlazorWebAssembly
using System.Net.Http;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
#elif BlazorServer
using Microsoft.AspNetCore.Builder;
#endif

namespace Bit.Client.Web.BlazorUI.Playground.Web;

public class Program
{
#if BlazorWebAssembly || BlazorServer
    public static async Task Main(string[] args)
    {
        await CreateHostBuilder(args)
            .RunAsync();
    }
#else
    public static void Main(string[] args)
    {
        System.Console.WriteLine("You're in blazor hybrid mode, please run app project instead of web project.");
    }
#endif

#if BlazorWebAssembly
    public static WebAssemblyHost CreateHostBuilder(string[] args)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault(args);
        builder.Services.AddSingleton(new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

        builder.Services.AddPlaygroundServices();

        return builder.Build();
    }
#elif BlazorServer
    public static WebApplication CreateHostBuilder(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        Startup.Services.Add(builder.Services, builder.Configuration);

        var app = builder.Build();

        Startup.Middlewares.Use(app, builder.Environment);

        return app;
    }
#endif
}
