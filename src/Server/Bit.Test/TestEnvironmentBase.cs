using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bit.Core;
using Bit.Core.Contracts;
using Bit.Core.Models;
using Bit.Owin.Implementations;
using Bit.Test.Core.Implementations;
using Bit.Test.Server;

namespace Bit.Test
{
    public class TestEnvironmentArgs
    {
        public string FullUri { get; set; } = null;

        public string HostName { get; set; }

        public bool UseRealServer { get; set; }

        public bool UseAspNetCore { get; set; }

        public bool UseHttps { get; set; }

        public Action<IDependencyManager> AdditionalDependencies { get; set; }

        public Action<AppEnvironment> ActiveAppEnvironmentCustomizer { get; set; }

        public IDependenciesManagerProvider CustomDependenciesManagerProvider { get; set; } = null;

        public IAppEnvironmentProvider CustomAppEnvironmentProvider { get; set; } = null;

        public bool UseProxyBasedDependencyManager { get; set; } = true;

        public int? Port { get; set; } = null;
    }

    public class TestEnvironmentBase : IDisposable
    {
        public TestEnvironmentBase(TestEnvironmentArgs args = null)
        {
            if (args == null)
                args = new TestEnvironmentArgs();

            if (args.FullUri == null && args.HostName == null)
                args.HostName = "localhost";

            string uri = args.FullUri != null ? args.FullUri : new Uri($"{(args.UseHttps ? "https" : "http")}://{args.HostName}:{args.Port}/").ToString();

            if (args.UseProxyBasedDependencyManager == true)
            {
                DefaultDependencyManager.Current = new AutofacTestDependencyManager();

                TestDependencyManager.CurrentTestDependencyManager.AutoProxyCreationIgnoreRules.AddRange(GetAutoProxyCreationIgnoreRules());
                TestDependencyManager.CurrentTestDependencyManager.AutoProxyCreationIncludeRules.AddRange(GetAutoProxyCreationIncludeRules());
            }

            DefaultDependenciesManagerProvider.Current = GetDependenciesManagerProvider(args);
            DefaultAppEnvironmentProvider.Current = GetAppEnvironmentProvider(args);

            Server = GetTestServer(args);

            Server.Initialize(uri);
        }

        protected virtual ITestServer GetTestServer(TestEnvironmentArgs args)
        {
            if (args.UseRealServer == true)
            {
                if (args.UseAspNetCore == false)
                    return new OwinSelfHostTestServer();
                else
                    return new AspNetCoreSelfHostTestServer();
            }
            else
            {
                if (args.UseAspNetCore == false)
                    return new OwinEmbeddedTestServer();
                else
                    return new AspNetCoreEmbeddedTestServer();
            }
        }

        protected virtual List<Func<TypeInfo, bool>> GetAutoProxyCreationIgnoreRules()
        {
            return new List<Func<TypeInfo, bool>>
            {
                serviceType => GetBaseTypes(serviceType).Any(t => t.Name == "DbContext"),
                serviceType => GetBaseTypes(serviceType).Any(t => t.Name == "Hub"),
                serviceType => serviceType.IsArray,
                serviceType => serviceType.IsInterface
            };
        }

        protected virtual List<Func<TypeInfo, bool>> GetAutoProxyCreationIncludeRules()
        {
            return new List<Func<TypeInfo, bool>>
            {
                serviceType => AssemblyContainer.Current.GetBitIdentityServerAssembly() == serviceType.Assembly,
                serviceType => AssemblyContainer.Current.GetBitCoreAssembly() == serviceType.Assembly,
                serviceType => AssemblyContainer.Current.GetBitOwinAssembly() == serviceType.Assembly,
                serviceType => AssemblyContainer.Current.GetBitDataAssembly() == serviceType.Assembly,
                serviceType => AssemblyContainer.Current.GetBitModelAssembly() == serviceType.Assembly,
                serviceType => AssemblyContainer.Current.GetBitTestAssembly() == serviceType.Assembly,
                serviceType => AssemblyContainer.Current.GetBitWebApiAssembly() == serviceType.Assembly,
                serviceType => AssemblyContainer.Current.GetBitODataAssembly() == serviceType.Assembly,
                serviceType => AssemblyContainer.Current.GetBitHangfireAssembly() == serviceType.Assembly,
                serviceType => AssemblyContainer.Current.GetBitSignalRAssembly() == serviceType.Assembly
            };
        }

        protected virtual IEnumerable<Type> GetBaseTypes(Type type)
        {
            Type baseType = type.BaseType;
            while (baseType != null)
            {
                yield return baseType;
                baseType = baseType.BaseType;
            }
        }

        protected virtual IAppEnvironmentProvider GetAppEnvironmentProvider(TestEnvironmentArgs args)
        {
            return args.CustomAppEnvironmentProvider ?? DefaultAppEnvironmentProvider.Current;
        }

        protected virtual IDependenciesManagerProvider GetDependenciesManagerProvider(TestEnvironmentArgs args)
        {
            return args.CustomDependenciesManagerProvider ?? DefaultDependenciesManagerProvider.Current;
        }

        public ITestServer Server { get; }

        public virtual void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            Server?.Dispose();
        }
    }
}
