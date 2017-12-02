using Bit.Core;
using Bit.Core.Contracts;
using Bit.Core.Implementations;
using Bit.Core.Models;
using Bit.Owin.Contracts;
using Bit.Owin.Implementations;
using Bit.OwinCore.Contracts;
using Bit.Test.Core.Implementations;
using Bit.Test.Server;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

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

    public class TestAdditionalDependencies : IAspNetCoreDependenciesManager, IOwinDependenciesManager, IDependenciesManagerProvider
    {
        private readonly Action<IDependencyManager> _dependenciesManager;

        public TestAdditionalDependencies(Action<IDependencyManager> dependenciesManager)
        {
            _dependenciesManager = dependenciesManager;
        }

        public virtual void ConfigureDependencies(IServiceProvider serviceProvider, IServiceCollection services, IDependencyManager dependencyManager)
        {
            _dependenciesManager?.Invoke(dependencyManager);
        }

        public virtual void ConfigureDependencies(IDependencyManager dependencyManager)
        {
            _dependenciesManager?.Invoke(dependencyManager);
        }

        public virtual IEnumerable<IDependenciesManager> GetDependenciesManagers()
        {
            yield return this;
        }
    }

    public class TestAppEnvironmentProvider : IAppEnvironmentProvider
    {
        private readonly IAppEnvironmentProvider _appEnvironmentProvider;
        private readonly Action<AppEnvironment> _appEnvCustomizer;

        protected TestAppEnvironmentProvider()
        {

        }

        public TestAppEnvironmentProvider(IAppEnvironmentProvider appEnvironmentProvider, Action<AppEnvironment> appEnvCustomizer = null)
        {
            if (appEnvironmentProvider == null)
                throw new ArgumentNullException(nameof(appEnvironmentProvider));

            _appEnvironmentProvider = appEnvironmentProvider;
            _appEnvCustomizer = appEnvCustomizer;
        }

        public virtual AppEnvironment GetActiveAppEnvironment()
        {
            AppEnvironment result = _appEnvironmentProvider.GetActiveAppEnvironment();

            _appEnvCustomizer?.Invoke(result);

            return result;
        }
    }

    public class TestEnvironmentBase : IDisposable
    {
        public TestEnvironmentBase(TestEnvironmentArgs args = null)
        {
            if (args == null)
                args = new TestEnvironmentArgs();

            if (args.FullUri == null && args.HostName == null)
                args.HostName = "localhost";

            string uri = args.FullUri ?? new Uri($"{(args.UseHttps ? "https" : "http")}://{args.HostName}:{args.Port}/").ToString();

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
                implementationType => GetBaseTypes(implementationType).Any(t => t.Name == "DbContext"),
                implementationType => GetBaseTypes(implementationType).Any(t => t.Name == "Hub"),
                implementationType => implementationType.IsArray
            };
        }

        protected virtual List<Func<TypeInfo, bool>> GetAutoProxyCreationIncludeRules()
        {
            return new List<Func<TypeInfo, bool>>
            {
                implementationType => AssemblyContainer.Current.GetBitIdentityServerAssembly() == implementationType.Assembly,
                implementationType => AssemblyContainer.Current.GetBitCoreAssembly() == implementationType.Assembly,
                implementationType => AssemblyContainer.Current.GetBitOwinAssembly() == implementationType.Assembly,
                implementationType => AssemblyContainer.Current.GetBitDataAssembly() == implementationType.Assembly,
                implementationType => AssemblyContainer.Current.GetBitModelAssembly() == implementationType.Assembly,
                implementationType => AssemblyContainer.Current.GetBitTestAssembly() == implementationType.Assembly,
                implementationType => AssemblyContainer.Current.GetBitWebApiAssembly() == implementationType.Assembly,
                implementationType => AssemblyContainer.Current.GetBitODataAssembly() == implementationType.Assembly,
                implementationType => AssemblyContainer.Current.GetBitHangfireAssembly() == implementationType.Assembly,
                implementationType => AssemblyContainer.Current.GetBitSignalRAssembly() == implementationType.Assembly
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
            return new TestAppEnvironmentProvider(args.CustomAppEnvironmentProvider ?? DefaultAppEnvironmentProvider.Current, args.ActiveAppEnvironmentCustomizer);
        }

        protected virtual IDependenciesManagerProvider GetDependenciesManagerProvider(TestEnvironmentArgs args)
        {
            return new CompositeDependenciesManagerProvider(args.CustomDependenciesManagerProvider ?? DefaultDependenciesManagerProvider.Current, new TestAdditionalDependencies(args.AdditionalDependencies));
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
