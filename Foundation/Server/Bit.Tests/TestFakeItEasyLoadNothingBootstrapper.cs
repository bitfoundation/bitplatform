using System.Collections.Generic;

namespace Foundation.Test
{
    public class TestFakeItEasyLoadNothingBootstrapper : FakeItEasyLoadNothingBootstrapperBase
    {
        public override IEnumerable<string> GetAssemblyFileNamesToScanForExtensions()
        {
            return base.GetAssemblyFileNamesToScanForExtensions();
        }
    }
}
