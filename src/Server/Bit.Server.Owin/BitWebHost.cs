using Bit.Core;
using Bit.Core.Contracts;
using Bit.Core.Implementations;
using Bit.Owin.Implementations;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Owin;
using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Bit.Owin
{
    public class BitWebHost
    {
        public static IHostBuilder CreateHost(string[] args)
        {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

            return Host.CreateDefaultBuilder(args)
                .ConfigureServices((context, services) =>
                {
                    DefaultDependencyManager.Current.Init();

                    AspNetCoreAppEnvironmentsProvider.Current.Configuration = context.Configuration;

                    AspNetCoreAppEnvironmentsProvider.Current.HostEnvironment = context.HostingEnvironment;

                    DefaultPathProvider.Current = new AspNetCorePathProvider(AspNetCoreAppEnvironmentsProvider.Current.HostEnvironment);

                    AspNetCoreAppEnvironmentsProvider.Current.Init();

                    services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
                    services.AddSingleton<OwinAppStartup, OwinAppStartup>();
                    services.Configure<KestrelServerOptions>(options => options.AddServerHeader = false);

                    if (DefaultDependencyManager.Current is IServiceCollectionAccessor dependencyManagerIServiceCollectionInterop)
                        dependencyManagerIServiceCollectionInterop.ServiceCollection = services;

                    foreach (IAppModule appModule in DefaultAppModulesProvider.Current.GetAppModules())
                    {
                        appModule.ConfigureDependencies(services, DefaultDependencyManager.Current);
                    }

                    HttpContext RegisterHttpContext(IDependencyResolver resolver)
                    {
                        throw new InvalidOperationException($"Please inject {nameof(IHttpContextAccessor)} instead of {nameof(HttpContext)}. See https://docs.microsoft.com/en-us/aspnet/core/performance/performance-best-practices?view=aspnetcore-3.0#do-not-store-ihttpcontextaccessorhttpcontext-in-a-field");
                    }

                    DefaultDependencyManager.Current.RegisterUsing(RegisterHttpContext, overwriteExisting: false);

                    IOwinContext RegisterOwinContext(IDependencyResolver resolver)
                    {
                        HttpContext? context = resolver.Resolve<IHttpContextAccessor>().HttpContext;

                        if (context == null)
                            throw new InvalidOperationException("http context is null");

                        if (!(context.Items["OwinContext"] is IOwinContext owinContext))
                            throw new InvalidOperationException("OwinContextIsNull");

                        return owinContext;
                    }

                    DefaultDependencyManager.Current.RegisterUsing(RegisterOwinContext, overwriteExisting: false);
                })
                .UseServiceProviderFactory(DefaultDependencyManager.Current)
                .ConfigureWebHost(webHostBuilder =>
                {
                    webHostBuilder.UseStartup<AspNetCoreAppStartup>();
                    webHostBuilder.CaptureStartupErrors(captureStartupErrors: true);
                    webHostBuilder.UseSetting(WebHostDefaults.DetailedErrorsKey, "true");
                });
        }

        public static IHostBuilder CreateWebHost(string[] args)
        {
            return CreateHost(args)
                .ConfigureWebHostDefaults(webHostBuilder =>
                {

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
