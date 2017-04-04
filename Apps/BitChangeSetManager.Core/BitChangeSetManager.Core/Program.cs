using Microsoft.AspNetCore.Hosting;

namespace BitChangeSetManager.Core
{
    public class Program
    {
        public static void Main(string[] args)
        {
            IWebHost host = new WebHostBuilder()
                .UseKestrel()
                .UseIISIntegration()
                .UseStartup<AppStartup>()
                .Build();

            host.Run();
        }
    }
}
