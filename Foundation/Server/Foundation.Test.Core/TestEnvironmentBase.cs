using Foundation.Api.Implementations;
using Foundation.Core.Contracts;
using Foundation.Core.Models;
using Foundation.Test.Core.Implementations;
using Foundation.Test.Server;
using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

namespace Foundation.Test
{
    public class TestEnvironmentArgs
    {
        public bool UseSso { get; set; } = false;

        public string FullUri { get; set; } = null;

        public string HostName { get; set; } = null;

        public bool UseRealServer { get; set; } = false;

        public bool UseHttps { get; set; } = false;

        public Action<IDependencyManager> AdditionalDependencies { get; set; } = null;

        public Action<AppEnvironment> ActiveAppEnvironmentCustomizer { get; set; } = null;

        public IDependenciesManagerProvider CustomDependenciesManagerProvider { get; set; } = null;

        public IAppEnvironmentProvider CustomAppEnvironmentProvider { get; set; } = null;

        public bool UseProxyBasedDependencyManager { get; set; } = true;

        public int? Port { get; set; } = null;

        public List<Func<TypeInfo, bool>> AutoProxyCreationIncludeRules { get; set; } = new List<Func<TypeInfo, bool>>();

        public List<Func<TypeInfo, bool>> AutoProxyCreationIgnoreRules { get; set; } = new List<Func<TypeInfo, bool>>();
    }

    public class TestEnvironmentBase : IDisposable
    {
        public TestEnvironmentBase(TestEnvironmentArgs args = null)
        {
            if (args == null)
                args = new TestEnvironmentArgs();

            if (args.FullUri == null && args.HostName == null)
                args.HostName = "127.0.0.1";

            string uri = args.FullUri != null ? args.FullUri : new Uri($"{(args.UseHttps ? "https" : "http")}://{args.HostName}:{args.Port}/").ToString();

            if (args.UseProxyBasedDependencyManager == true)
            {
                DefaultDependencyManager.Current = new AutofacTestDependencyManager();

                TestDependencyManager.CurrentTestDependencyManager.AutoProxyCreationIncludeRules.AddRange(args.AutoProxyCreationIncludeRules.Union(new Func<TypeInfo, bool>[]
                {
                    serviceType => GetType().GetTypeInfo().Assembly == serviceType.Assembly
                }));

                TestDependencyManager.CurrentTestDependencyManager.AutoProxyCreationIgnoreRules.AddRange(args.AutoProxyCreationIgnoreRules);
            }

            DefaultDependenciesManagerProvider.Current = args.CustomDependenciesManagerProvider ?? DependenciesManagerProviderBuilder(args);
            DefaultAppEnvironmentProvider.Current = args.CustomAppEnvironmentProvider ?? AppEnvironmentProviderBuilder(args);

            if (args.UseRealServer == true)
                Server = TestServerFactory.GetSelfHostTestServer();
            else
                Server = TestServerFactory.GetEmbeddedTestServer();

            Server.Initialize(uri);
        }

        public static Func<TestEnvironmentArgs, IAppEnvironmentProvider> AppEnvironmentProviderBuilder { get; set; } = args => DefaultAppEnvironmentProvider.Current;

        public static Func<TestEnvironmentArgs, IDependenciesManagerProvider> DependenciesManagerProviderBuilder { get; set; } = args => DefaultDependenciesManagerProvider.Current;

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
