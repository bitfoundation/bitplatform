using Foundation.Test;
using IdentityServer.Api.Contracts;
using IdentityServer.Test.Api.Implementations;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace IdentityServer.Test
{
    public class IdentityServerTestEnvironment : TestEnvironmentBase
    {
        public IdentityServerTestEnvironment(TestEnvironmentArgs args)
            : base(args)
        {

        }

        public IdentityServerTestEnvironment(bool useProxyBasedDependencyManager = true) :
            this(new TestEnvironmentArgs
            {
                UseHttps = false,
                UseRealServer = true,
                UseProxyBasedDependencyManager = useProxyBasedDependencyManager,
                Port = 8080,
                CustomDependenciesManagerProvider = new IdentityServerTestDependenciesManagerProvider(),
                CustomAppEnvironmentProvider = new IdentityServerTestAppEnvironmentProvider(),
                AutoProxyCreationIncludeRules = new List<Func<TypeInfo, bool>>
                {
                    serviceType => serviceType.Assembly == typeof(IdentityServerTestEnvironment).Assembly,
                    serviceType => serviceType.Assembly == typeof(IClientProvider).Assembly
                }
            })
        {

        }
    }
}
