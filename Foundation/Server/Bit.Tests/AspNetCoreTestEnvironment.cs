using Foundation.AspNetCore.Test.Api.Implementations;
using Foundation.AspNetCore.Test.Server;
using Foundation.Core.Contracts;
using Foundation.Test;

namespace Foundation.AspNetCore.Test
{
    public class AspNetCoreTestEnvironment : TestEnvironment
    {
        public AspNetCoreTestEnvironment(TestEnvironmentArgs args = null)
            : base(args)
        {

        }

        protected override ITestServer GetTestServer(TestEnvironmentArgs args)
        {
            if (args.UseRealServer == true)
                return new AspNetCoreSelfHostTestServer();
            else
                return new AspNetCoreEmbeddedTestServer();
        }

        protected override IDependenciesManagerProvider GetDependenciesManagerProvider(TestEnvironmentArgs args)
        {
            return args.CustomDependenciesManagerProvider ?? new FoundationAspNetCoreTestServerDependenciesManagerProvider(args);
        }
    }
}
