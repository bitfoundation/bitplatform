using Bit.OwinCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace CustomerDtoControllerSample
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            await BuildWebHost(args)
                .RunAsync();
        }

        public static IWebHost BuildWebHost(string[] args) =>
            BitWebHost.CreateDefaultBuilder(args)
            .UseConfiguration(new ConfigurationBuilder().AddEnvironmentVariables().Build())
                .UseStartup<AppStartup>()
                .Build();
    }
}
