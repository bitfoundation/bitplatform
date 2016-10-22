using Foundation.AspNetCore.Test.Api.Implementations;
using Foundation.AspNetCore.Test.Server;
using Foundation.Test;
using Foundation.Test.Server;

namespace Foundation.AspNetCore.Test
{
    public class AspNetCoreTestEnvironment : TestEnvironmentBase
    {
        public AspNetCoreTestEnvironment(TestEnvironmentArgs args = null)
            : base(args)
        {

        }

        static AspNetCoreTestEnvironment()
        {
            TestServerFactory.GetSelfHostTestServer = () =>
            {
                return new AspNetCoreSelfHostTestServer();
            };

            TestServerFactory.GetEmbeddedTestServer = () =>
            {
                return new AspNetCoreEmbeddedTestServer();
            };

            DependenciesManagerProviderBuilder = (args) => new FoundationAspNetCoreTestServerDependenciesManagerProvider(args);
            AppEnvironmentProviderBuilder = (args) => new FoundationAspNetCoreAppEnvironmentPrvider(args);
        }
    }
}
