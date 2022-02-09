#if BlazorServer
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
#elif BlazorWebAssembly
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
#endif

namespace TodoTemplate.App;

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

            builder.Services.AddSingleton(c => new HttpClient(c.GetRequiredService<TodoTemplateHttpClientHandler>()) { BaseAddress = new Uri($"{builder.HostEnvironment.BaseAddress}api/") });
            builder.Services.AddTransient<ITokenProvider, ClientSideTokenProvider>();

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
