using Bit.Core;
using Bit.Core.Contracts;
using Bit.Core.Implementations;
using Bit.Core.Models;
using Bit.Owin.Implementations;
using Bit.Test.Implementations;
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

#if DotNet
        public bool UseAspNetCore { get; set; } = true;
#else
        public bool UseAspNetCore { get; set; } = true;
#endif

        public bool UseHttps { get; set; }

        public Action<IDependencyManager, IServiceCollection> AdditionalDependencies { get; set; }

        public Action<AppEnvironment> ActiveAppEnvironmentCustomizer { get; set; }

        public IAppModulesProvider CustomAppModulesProvider { get; set; } = null;

        public IAppEnvironmentsProvider CustomAppEnvironmentsProvider { get; set; } = null;

        public bool UseProxyBasedDependencyManager { get; set; } = true;

        public int? Port { get; set; } = null;
    }

    public class TestAdditionalDependencies : IAppModule, IAppModulesProvider
    {
        private readonly Action<IDependencyManager, IServiceCollection> _dependencyManagerDelegate;

        public TestAdditionalDependencies(Action<IDependencyManager, IServiceCollection> dependencyManagerDelegate)
        {
            _dependencyManagerDelegate = dependencyManagerDelegate;
        }

        public virtual void ConfigureDependencies(IServiceCollection services, IDependencyManager dependencyManager)
        {
            _dependencyManagerDelegate?.Invoke(dependencyManager, services);
        }

        public virtual IEnumerable<IAppModule> GetAppModules()
        {
            yield return this;
        }
    }

    public class TestAppEnvironmentsProvider : IAppEnvironmentsProvider
    {
        private readonly IAppEnvironmentsProvider _appEnvironmentsProvider;
        private readonly Action<AppEnvironment> _appEnvCustomizer;

        protected TestAppEnvironmentsProvider()
        {

        }

        public TestAppEnvironmentsProvider(IAppEnvironmentsProvider appEnvironmentProvider, Action<AppEnvironment> appEnvCustomizer = null)
        {
            if (appEnvironmentProvider == null)
                throw new ArgumentNullException(nameof(appEnvironmentProvider));

            _appEnvironmentsProvider = appEnvironmentProvider;
            _appEnvCustomizer = appEnvCustomizer;
        }

        public virtual AppEnvironment GetActiveAppEnvironment()
        {
            AppEnvironment result = _appEnvironmentsProvider.GetActiveAppEnvironment();

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

            DefaultAppModulesProvider.Current = GetAppModulesProvider(args);
            DefaultAppEnvironmentsProvider.Current = GetAppEnvironmentsProvider(args);

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
                implementationType => GetBaseTypes(implementationType).Any(t => t.Name == "Profile"), /*AutoMapper*/
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

        protected virtual IAppEnvironmentsProvider GetAppEnvironmentsProvider(TestEnvironmentArgs args)
        {
            return new TestAppEnvironmentsProvider(args.CustomAppEnvironmentsProvider ?? DefaultAppEnvironmentsProvider.Current, args.ActiveAppEnvironmentCustomizer);
        }

        protected virtual IAppModulesProvider GetAppModulesProvider(TestEnvironmentArgs args)
        {
            return new CompositeAppModulesProvider(args.CustomAppModulesProvider ?? DefaultAppModulesProvider.Current, new TestAdditionalDependencies(args.AdditionalDependencies));
        }

        public ITestServer Server { get; }

        public virtual void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            Server?.Dispose();
        }
    }
}
