using Microsoft.AspNetCore.Hosting;
using System.IO;

namespace BitChangeSetManager.Core
{
    public class Program
    {
        public static void Main(string[] args)
        {
            IWebHost host = new WebHostBuilder()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseKestrel(options =>
                {
                    options.AddServerHeader = false;
                })
                .UseIISIntegration()
                .CaptureStartupErrors(true)
                .UseStartup<AppStartup>()
                .Build();

            host.Run();
        }
    }
}
