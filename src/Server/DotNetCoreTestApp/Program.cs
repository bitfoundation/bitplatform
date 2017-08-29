using Microsoft.AspNetCore.Hosting;
using System;
using System.IO;
using System.Reflection;

namespace DotNetCoreTestApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

            IWebHost host = new WebHostBuilder()
                .UseKestrel(options =>
                {
                    options.AddServerHeader = false;
                })
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseWebRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration()
                .UseStartup<AppStartup>()
                .CaptureStartupErrors(true)
                .Build();

            host.Run();
        }

        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            if (args.Name.StartsWith("System.Data.Linq"))
                return Assembly.LoadFile(@"C:\Program Files\Mono\lib\mono\2.0-api\System.Data.Linq.dll");
            return null;
        }
    }
}
