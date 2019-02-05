using Bit.Core;
using Microsoft.AspNetCore.Hosting;
using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Bit.OwinCore
{
    public class BitWebHost
    {
        public static IWebHostBuilder CreateDefaultBuilder(string[] args)
        {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

            Version aspNetCoreVersion = typeof(IWebHostBuilder).Assembly.GetName().Version;

            if (aspNetCoreVersion.Major > 1) // asp.net core 2.0+
            {
                string bitOwinCoreAsmName = null;

                if (aspNetCoreVersion.Major == 3) // asp.net core 3.0
                {
                    bitOwinCoreAsmName = "Bit.OwinCore.AspNetCore22Upgrade";
                }
                else if (aspNetCoreVersion.Major == 2) // asp.net core 2.0
                {
                    bitOwinCoreAsmName = aspNetCoreVersion.Minor == 0 ? "Bit.OwinCore.AspNetCore2Upgrade" : $"Bit.OwinCore.AspNetCore2{aspNetCoreVersion.Minor}Upgrade";
                }
                else
                {
                    throw new NotSupportedException($"ASP.NET Core {aspNetCoreVersion.Major}.{aspNetCoreVersion.Minor} is not supported.");
                }

                return (IWebHostBuilder)Assembly.Load(bitOwinCoreAsmName)
                    .GetType($"{bitOwinCoreAsmName}.BitWebHost")
                    .GetMethod("CreateDefaultBuilder")
                    .Invoke(null, new object[] { args });
            }
            else // asp.net core 1.0
            {
                return AspNetCore1();
            }
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

        private static IWebHostBuilder AspNetCore1()
        {
            return new WebHostBuilder()
                .UseKestrel(options =>
                {
                    options.AddServerHeader = false;
                })
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration()
                .CaptureStartupErrors(captureStartupErrors: true)
                .UseSetting(WebHostDefaults.DetailedErrorsKey, "true");
        }
    }
}
