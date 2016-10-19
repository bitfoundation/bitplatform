using System.Collections.Generic;

namespace Foundation.Test
{
    public class TestFakeItEasyLoadNothingBootstrapper : FakeItEasyLoadNothingBootstrapperBase
    {
        public TestFakeItEasyLoadNothingBootstrapper()
        {

        }

        public virtual IEnumerable<string> GetAssemblyFileNamesToScanForExtensions()
        {
            return new string[] { };
        }
    }
}
