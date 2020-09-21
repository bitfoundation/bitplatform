using Bit.Core;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Bit.Owin
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

        static Assembly? CurrentDomain_AssemblyResolve(object? sender, ResolveEventArgs args)
        {
            if (args == null)
                throw new ArgumentNullException(nameof(args));

            Assembly bitOwinCoreAssembly = AssemblyContainer.Current.GetServerOwinAssembly();

            string? dllResourceName = bitOwinCoreAssembly.GetManifestResourceNames().ExtendedSingleOrDefault($"Finding equivalent resource for {args.Name}", resName => resName == $"Bit.Owin.Assemblies.{new AssemblyName(args.Name!).Name}.dll");

            if (!string.IsNullOrEmpty(dllResourceName))
            {
                using (Stream dllStream = bitOwinCoreAssembly.GetManifestResourceStream(dllResourceName)!)
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
