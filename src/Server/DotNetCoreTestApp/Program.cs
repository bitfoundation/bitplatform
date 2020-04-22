using Bit.Owin;
using Bit.Owin.Implementations;
using Microsoft.AspNetCore.Hosting;
using Serilog;
using Serilog.Formatting.Compact;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetCoreTestApp
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console(new RenderedCompactJsonFormatter())
                .Enrich.FromLogContext()
                .Enrich.With<SerilogLogStore>()
                .CreateLogger();

            Log.Logger.Information("Starting app...");

            try
            {
                await BuildWebHost(args)
                    .RunAsync();
            }
            catch (Exception exp)
            {
                Log.Fatal(exp, "Host terminated unexpectedly");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        public static IWebHost BuildWebHost(string[] args) =>
            BitWebHost.CreateDefaultBuilder(args)
                .UseStartup<AppStartup>()
                .ConfigureServices(services => services.AddSingleton(Log.Logger))
                .UseSerilog()
                .Build();
    }
}
