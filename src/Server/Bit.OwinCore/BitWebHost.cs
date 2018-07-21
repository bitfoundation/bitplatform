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

            TypeInfo webHostType = Assembly.Load("Microsoft.AspNetCore").GetType($"Microsoft.AspNetCore.WebHost").GetTypeInfo();

            AssemblyName msAspNetCoreAssemblyName = Assembly.Load("Microsoft.AspNetCore").GetName();

            if (msAspNetCoreAssemblyName.Version.Major == 2) // asp.net core 2.X.X
            {
                if (msAspNetCoreAssemblyName.Version.Minor == 0) // asp.net core 2.0.X
                {
                    Assembly.Load("Bit.OwinCore.AspNetCore2Upgrade");
                }
                else if (msAspNetCoreAssemblyName.Version.Minor == 1) // asp.net core 2.1.X
                {
                    Assembly.Load("Bit.OwinCore.AspNetCore21Upgrade");
                }
            }

            MethodInfo createDefaultBuilderMethod = webHostType?.GetMethods().ExtendedSingleOrDefault("Finding CreateDefaultBuilder method", m => m.Name == "CreateDefaultBuilder" && m.IsGenericMethod == false && m.GetParameters().Select(p => p.ParameterType).SequenceEqual(new Type[] { typeof(string[]).GetTypeInfo() }));

            if (createDefaultBuilderMethod == null)
            {
                return AspNetCore1();
            }
            else
            {
                return AspNetCore2(args, createDefaultBuilderMethod);
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

        private static IWebHostBuilder AspNetCore2(string[] args, MethodInfo createDefaultBuilderMethod)
        {
            return ((IWebHostBuilder)createDefaultBuilderMethod.Invoke(null, new object[] { args }))
                .CaptureStartupErrors(captureStartupErrors: true)
                .UseSetting(WebHostDefaults.DetailedErrorsKey, "true")
                /*.UseKestrel(options =>
                {
                    options.AddServerHeader = false;
                })*/;
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
