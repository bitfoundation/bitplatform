using Bit.OwinCore;
using Microsoft.AspNetCore.Hosting;

namespace BitChangeSetManager.Core
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BuildWebHost(args).Run();
        }

        public static IWebHost BuildWebHost(string[] args) =>
            BitWebHost.CreateDefaultBuilder(args)
                .UseStartup<AppStartup>()
                .Build();
    }
}
