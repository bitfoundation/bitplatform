using Bit.Core;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Internal;
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

            TypeInfo webHostType = Type.GetType($"Microsoft.AspNetCore.WebHost, Microsoft.AspNetCore, Version={typeof(WebHost).GetTypeInfo().Assembly.GetName().Version}, Culture=neutral, PublicKeyToken=adb9793829ddae60")?.GetTypeInfo();

            MethodInfo createDefaultBuilderMethod = webHostType?.GetMethod("CreateDefaultBuilder", new Type[] { typeof(string[]).GetTypeInfo() });

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
                .CaptureStartupErrors(captureStartupErrors: true);
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
                .CaptureStartupErrors(captureStartupErrors: true);
        }
    }
}
