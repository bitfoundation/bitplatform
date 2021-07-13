using Bit.Core;
using Bit.Core.Implementations;
using Bit.Owin;
using Bit.Owin.Implementations;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Formatting.Compact;
using System;
using System.Threading.Tasks;

namespace DotNetTestApp
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            AssemblyContainer.Current.Init();

            AspNetCoreAppEnvironmentsProvider.Current.Use();

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

        public static IHost BuildWebHost(string[] args) =>
            BitWebHost.CreateWebHost(args)
                .UseSerilog()
                .Build();
    }
}
