using Bit.OwinCore;
using Microsoft.AspNetCore.Hosting;
using System;
using System.Reflection;

namespace DotNetCoreTestApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

            BuildWebHost(args).Run();
        }

        public static IWebHost BuildWebHost(string[] args) =>
            BitWebHost.CreateDefaultBuilder(args)
                .UseStartup<AppStartup>()
                .Build();

        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            if (args.Name.StartsWith("System.Data.Linq"))
                return Assembly.LoadFile(@"C:\Program Files\Mono\lib\mono\2.0-api\System.Data.Linq.dll");
            else if (args.Name.StartsWith("System.IdentityModel"))
                return Assembly.LoadFile(@"C:\Program Files\Mono\lib\mono\4.5\System.IdentityModel.dll");
            return null;
        }
    }
}
