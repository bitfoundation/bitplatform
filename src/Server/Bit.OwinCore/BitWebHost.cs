using Bit.Core;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.OwinCore
{
    public class BitWebHost
    {
        public static IWebHostBuilder CreateDefaultBuilder(string[] args)
        {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

            return WebHost.CreateDefaultBuilder(args)
                .CaptureStartupErrors(captureStartupErrors: true)
                .UseSetting(WebHostDefaults.DetailedErrorsKey, "true")
                .ConfigureServices(services =>
                {
                    services.Configure<KestrelServerOptions>(options => options.AddServerHeader = false);
                });
        }

        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            Assembly bitOwinCoreAssembly = AssemblyContainer.Current.GetBitOwinCoreAssembly();

            string dllResourceName = bitOwinCoreAssembly.GetManifestResourceNames().ExtendedSingleOrDefault($"Finding equivalent resource for {args.Name}", resName => resName == $"Bit.OwinCore.Assemblies.{new AssemblyName(args.Name).Name}.dll");

            if (!string.IsNullOrEmpty(dllResourceName))
            {
                using (Stream dllStream = bitOwinCoreAssembly.GetManifestResourceStream(dllResourceName))
                {
                    using (MemoryStream bufferStream = new MemoryStream())
                    {
                        dllStream.CopyTo(bufferStream);
                        return Assembly.Load(bufferStream.ToArray());
                    }
                }
            }

            return null;
        }
    }
}
