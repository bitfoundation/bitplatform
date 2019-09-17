using Bit.OwinCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace CustomerDtoControllerSample
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BuildWebHost(args).Run();
        }

        public static IWebHost BuildWebHost(string[] args) =>
            BitWebHost.CreateDefaultBuilder(args)
            .UseConfiguration(new ConfigurationBuilder().AddEnvironmentVariables().Build())
                .UseStartup<AppStartup>()
                .Build();
    }
}
