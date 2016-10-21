using Foundation.Test;
using IdentityServer.Api.Contracts;
using IdentityServer.Test.Api.Implementations;

namespace IdentityServer.Test
{
    public class IdentityServerTestEnvironment : TestEnvironment
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
                AutoCreateProxyForAssembliesInThisList = new[] { typeof(IdentityServerTestEnvironment).Assembly, typeof(IClientProvider).Assembly }
            })
        {

        }
    }
}
