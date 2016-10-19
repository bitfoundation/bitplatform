using Foundation.Api.Implementations;
using Foundation.Core.Contracts;
using Foundation.Core.Models;
using Foundation.Test.Api.Implementations;
using Foundation.Test.Core.Implementations;
using Foundation.Test.Server;
using System;
using System.Reflection;

namespace Foundation.Test
{
    public class TestEnvironmentArgs
    {
        public bool UseSso { get; set; } = false;

        public Uri FullUri { get; set; } = null;

        public string HostName { get; set; } = null;

        public bool UseRealServer { get; set; } = false;

        public bool UseHttps { get; set; } = false;

        public Action<IDependencyManager> AdditionalDependencies { get; set; } = null;

        public Action<AppEnvironment> ActiveAppEnvironmentCustomizer { get; set; } = null;

        public IDependenciesManagerProvider CustomDependenciesManagerProvider { get; set; } = null;

        public IAppEnvironmentProvider CustomAppEnvironmentProvider { get; set; } = null;

        public bool UseProxyBasedDependencyManager { get; set; } = true;

        public int? Port { get; set; } = null;

        public Assembly[] AutoCreateProxyForAssembliesInThisList { get; set; } = new Assembly[] { };
    }

    public class TestEnvironment : IDisposable
    {
        public TestEnvironment(TestEnvironmentArgs args = null)
        {
            if (args == null)
                args = new TestEnvironmentArgs();

            if (args.FullUri == null && args.HostName == null)
                args.HostName = "127.0.0.1";

            Uri uri = args.FullUri != null ? args.FullUri : new Uri($"{(args.UseHttps ? "https" : "http")}://{args.HostName}:{args.Port}/");

            if (args.UseProxyBasedDependencyManager == true)
            {
                DefaultDependencyManager.Current = new AutofacTestDependencyManager();
                TestDependencyManager.CurrentTestDependencyManager.AutoProxyCreateForTypesFromAssembliesInThisList.AddRange(args.AutoCreateProxyForAssembliesInThisList);
            }

            DefaultDependenciesManagerProvider.Current = args.CustomDependenciesManagerProvider ?? new TestDependenciesManagerProvider(args.AdditionalDependencies, useSso: args.UseSso);

            DefaultAppEnvironmentProvider.Current = args.CustomAppEnvironmentProvider ?? new TestAppEnvironmentProvider(args.ActiveAppEnvironmentCustomizer);

            if (args.UseRealServer == true)
                Server = TestServerFactory.GetSelfHostTestServer();
            else
                Server = TestServerFactory.GetEmbeddedTestServer();

            Server.Initialize(uri);
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