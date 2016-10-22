using Foundation.Test.Api.Implementations;

namespace Foundation.Test
{
    public class TestEnvironment : TestEnvironmentBase
    {
        static TestEnvironment()
        {
            AppEnvironmentProviderBuilder = args => new TestAppEnvironmentProvider(args);
            DependenciesManagerProviderBuilder = args => new TestDependenciesManagerProvider(args);
        }

        public TestEnvironment(TestEnvironmentArgs args = null)
            : base(args)
        {
        }
    }
}