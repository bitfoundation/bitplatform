using Microsoft.AspNetCore.Hosting;
using System.IO;

namespace WebApiAspNetCoreHost
{
    public class Program
    {
        public static void Main(string[] args)
        {
            IWebHost host = new WebHostBuilder()
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration()
                .UseStartup<AppStartup>()
                .Build();

            host.Run();
        }
    }
}
