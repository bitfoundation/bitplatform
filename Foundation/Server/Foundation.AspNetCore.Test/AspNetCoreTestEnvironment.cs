using Foundation.AspNetCore.Test.Api.Implementations;
using Foundation.AspNetCore.Test.Server;
using Foundation.Test;
using Foundation.Test.Server;

namespace Foundation.AspNetCore.Test
{
    public class AspNetCoreTestEnvironment : TestEnvironment
    {
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
        }

        public AspNetCoreTestEnvironment(TestEnvironmentArgs args = null)
            : base(ApplyDefaults(args))
        {

        }

        private static TestEnvironmentArgs ApplyDefaults(TestEnvironmentArgs args)
        {
            if (args == null)
                args = new TestEnvironmentArgs();

            args.UseSso = false;

            if (args.CustomDependenciesManagerProvider == null)
                args.CustomDependenciesManagerProvider = new FoundationAspNetCoreTestServerDependenciesManagerProvider(args);

            return args;
        }
    }
}
