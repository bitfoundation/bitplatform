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
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace Bit.Test
{
    public class TestClientArgs
    {
        public string AppVersion { get; set; } = "1.0";

        public string ClientScreenSize { get; set; } = "DesktopAndTablet";

        public string Culture { get; set; } = CultureInfo.CurrentUICulture.Name;

        public string CurrentTimeZone { get; set; } = TimeZoneInfo.Local.Id;

        public string DesiredTimeZone { get; set; } = TimeZoneInfo.Local.Id;
    }

    public class TestEnvironmentArgs
    {
        public string? FullUri { get; set; }

        public string? HostName { get; set; }

        public bool UseRealServer { get; set; }

        public bool UseHttps { get; set; }

        public int? Port { get; set; }

        public TestClientArgs ClientArgs { get; set; } = new TestClientArgs { };

        public Action<IDependencyManager, IServiceCollection>? AdditionalDependencies { get; set; }

        public Action<AppEnvironment>? ActiveAppEnvironmentCustomizer { get; set; }

        public IAppModulesProvider? CustomAppModulesProvider { get; set; }

        public IAppEnvironmentsProvider? CustomAppEnvironmentsProvider { get; set; }

        public bool UseTestDependencyManager { get; set; } = true;
    }

    public class TestAdditionalDependencies : IAppModule, IAppModulesProvider
    {
        private readonly Action<IDependencyManager, IServiceCollection>? _dependencyManagerDelegate;

        public TestAdditionalDependencies(Action<IDependencyManager, IServiceCollection>? dependencyManagerDelegate)
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
        private readonly IAppEnvironmentsProvider _appEnvironmentsProvider = default!;
        private readonly Action<AppEnvironment>? _appEnvCustomizer;

        protected TestAppEnvironmentsProvider()
        {

        }

        public TestAppEnvironmentsProvider(IAppEnvironmentsProvider appEnvironmentProvider, Action<AppEnvironment>? appEnvCustomizer = null)
        {
            _appEnvironmentsProvider = appEnvironmentProvider ?? throw new ArgumentNullException(nameof(appEnvironmentProvider));
            _appEnvCustomizer = appEnvCustomizer;
        }

        public virtual AppEnvironment GetActiveAppEnvironment()
        {
            var (success, message) = TryGetActiveAppEnvironment(out AppEnvironment? activeAppEnvironment);
            if (success == true)
                return activeAppEnvironment!;
            throw new InvalidOperationException(message);
        }

        public virtual (bool success, string? message) TryGetActiveAppEnvironment(out AppEnvironment? activeAppEnvironment)
        {
            try
            {
                activeAppEnvironment = _appEnvironmentsProvider.GetActiveAppEnvironment();

                _appEnvCustomizer?.Invoke(activeAppEnvironment);

                return (true, null);
            }
            catch (Exception exp)
            {
                activeAppEnvironment = null;
                return (false, exp.Message);
            }
        }
    }

    public class TestEnvironmentBase : IDisposable
    {
        public TestEnvironmentBase(TestEnvironmentArgs? args = null)
        {
            if (args == null)
                args = new TestEnvironmentArgs();

            if (args.FullUri == null && args.HostName == null)
                args.HostName = "localhost";

            string uri = args.FullUri ?? new Uri($"{(args.UseHttps ? "https" : "http")}://{args.HostName}:{args.Port}/").ToString();

            if (args.UseTestDependencyManager == true)
            {
                DefaultDependencyManager.Current = new AutofacTestDependencyManager();

                TestDependencyManager.CurrentTestDependencyManager.AutoProxyCreationIgnoreRules.AddRange(GetAutoProxyCreationIgnoreRules());
                TestDependencyManager.CurrentTestDependencyManager.AutoProxyCreationIncludeRules.AddRange(GetAutoProxyCreationIncludeRules());
            }
            else
            {
                DefaultDependencyManager.Current = new AutofacDependencyManager();
            }

            DefaultAppModulesProvider.Current = GetAppModulesProvider(args);
            DefaultAppEnvironmentsProvider.Current = GetAppEnvironmentsProvider(args);

            Server = GetTestServer(args);

            Server.Initialize(uri);
        }

        protected virtual ITestServer GetTestServer(TestEnvironmentArgs args)
        {
            if (args == null)
                throw new ArgumentNullException(nameof(args));

            if (args.UseRealServer == true)
            {
                return new AspNetCoreSelfHostTestServer(args);
            }
            else
            {
                return new AspNetCoreEmbeddedTestServer(args);
            }
        }

        protected virtual List<Func<TypeInfo, bool>> GetAutoProxyCreationIgnoreRules()
        {
            return new List<Func<TypeInfo, bool>>
            {
                implementationType => typeof(IServiceProvider).IsAssignableFrom(implementationType),
                implementationType => GetBaseTypes(implementationType).Any(t => t.Name == "DbContext"),
                implementationType => GetBaseTypes(implementationType).Any(t => t.Name == "Hub"),
                implementationType => GetBaseTypes(implementationType).Any(t => t.Name == "Profile"), /*AutoMapper*/
                implementationType => GetBaseTypes(implementationType).Any(t => t.Name == "ODataEdmTypeSerializer"),
                implementationType => GetBaseTypes(implementationType).Any(t => t.Name == "DefaultODataDeserializerProvider"),
                implementationType => GetBaseTypes(implementationType).Any(t => t.Name.Contains("AuthenticationHandler", StringComparison.InvariantCulture)),
                implementationType => implementationType == typeof(AspNetCoreAppEnvironmentsProvider).GetTypeInfo(),
                implementationType => implementationType.IsArray
            };
        }

        protected virtual List<Func<TypeInfo, bool>> GetAutoProxyCreationIncludeRules()
        {
            return new List<Func<TypeInfo, bool>>
            {
                implementationType => AssemblyContainer.Current.GetServerCoreAssembly() == implementationType.Assembly,
                implementationType => AssemblyContainer.Current.GetServerDataAssembly() == implementationType.Assembly,
                implementationType => AssemblyContainer.Current.GetServerDataEntityFrameworkCoreAssembly() == implementationType.Assembly,
                implementationType => AssemblyContainer.Current.GetServerHangfireAssembly() == implementationType.Assembly,
                implementationType => AssemblyContainer.Current.GetServerIdentityServerAssembly() == implementationType.Assembly,
                implementationType => AssemblyContainer.Current.GetServerMetadataAssembly() == implementationType.Assembly,
                implementationType => AssemblyContainer.Current.GetServerODataAssembly() == implementationType.Assembly,
                implementationType => AssemblyContainer.Current.GetServerOwinAssembly() == implementationType.Assembly,
                implementationType => AssemblyContainer.Current.GetServerSignalRAssembly() == implementationType.Assembly,
                implementationType => AssemblyContainer.Current.GetServerTestAssembly() == implementationType.Assembly,
                implementationType => AssemblyContainer.Current.GetServerWebApiAssembly() == implementationType.Assembly,
                implementationType => AssemblyContainer.Current.GetUniversalCoreAssembly() == implementationType.Assembly,
                implementationType => AssemblyContainer.Current.GetUniversalDataAssembly() == implementationType.Assembly,
                implementationType => AssemblyContainer.Current.GetUniversalModelAssembly() == implementationType.Assembly
            };
        }

        protected virtual IEnumerable<Type> GetBaseTypes(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            Type? baseType = type.BaseType;
            while (baseType != null)
            {
                yield return baseType;
                baseType = baseType.BaseType;
            }
        }

        protected virtual IAppEnvironmentsProvider GetAppEnvironmentsProvider(TestEnvironmentArgs args)
        {
            if (args == null)
                throw new ArgumentNullException(nameof(args));

            return new TestAppEnvironmentsProvider(args.CustomAppEnvironmentsProvider ?? DefaultAppEnvironmentsProvider.Current, args.ActiveAppEnvironmentCustomizer);
        }

        protected virtual IAppModulesProvider GetAppModulesProvider(TestEnvironmentArgs args)
        {
            if (args == null)
                throw new ArgumentNullException(nameof(args));

            return new CompositeAppModulesProvider(args.CustomAppModulesProvider ?? DefaultAppModulesProvider.Current, new TestAdditionalDependencies(args.AdditionalDependencies));
        }

        public virtual ITestServer Server { get; }

        public virtual IDependencyResolver DependencyResolver => DefaultDependencyManager.Current;

        public virtual IEnumerable<T> GetObjects<T>()
        {
            return TestDependencyManager.CurrentTestDependencyManager
                .Objects
                .OfType<T>()
                .Distinct();
        }

        private bool isDisposed;

        public virtual void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (isDisposed) return;
            Server?.Dispose();
            isDisposed = true;
        }

        ~TestEnvironmentBase()
        {
            Dispose(false);
        }
    }
}
