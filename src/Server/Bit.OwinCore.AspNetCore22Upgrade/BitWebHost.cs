using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.OwinCore.AspNetCore22Upgrade
{
    public class BitWebHost
    {
        public static IWebHostBuilder CreateDefaultBuilder(string[] args)
        {
            return WebHost.CreateDefaultBuilder(args)
                .CaptureStartupErrors(captureStartupErrors: true)
                .UseSetting(WebHostDefaults.DetailedErrorsKey, "true")
                .ConfigureServices(services =>
                {
                    services.Configure<KestrelServerOptions>(options => options.AddServerHeader = false);
                });
        }
    }
}
