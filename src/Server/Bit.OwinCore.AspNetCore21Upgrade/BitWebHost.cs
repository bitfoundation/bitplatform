using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace Bit.OwinCore.AspNetCore21Upgrade
{
    public class BitWebHost
    {
        public static IWebHostBuilder CreateDefaultBuilder(string[] args)
        {
            return WebHost.CreateDefaultBuilder(args)
                .CaptureStartupErrors(captureStartupErrors: true)
                .UseSetting(WebHostDefaults.DetailedErrorsKey, "true")
                .UseKestrel(options =>
                {
                    options.AllowSynchronousIO = false;
                    options.AddServerHeader = false;
                });
        }
    }
}
