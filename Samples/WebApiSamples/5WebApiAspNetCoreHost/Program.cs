using Microsoft.AspNetCore.Hosting;
using System.IO;

namespace WebApiAspNetCoreHost
{
    public class Program
    {
        public static void Main(string[] args)
        {
            IWebHost host = new WebHostBuilder()
                .UseKestrel(options =>
                {
                    options.AddServerHeader = false;
                })
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration()
                .UseStartup<AppStartup>()
                .CaptureStartupErrors(true)
                .Build();

            host.Run();
        }
    }
}
