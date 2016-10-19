using System.Collections.Generic;

namespace Foundation.Test
{
    public class AspNetCoreFakeItEasyLoadNothingBootstrapper : FakeItEasyLoadNothingBootstrapperBase
    {
        public override IEnumerable<string> GetAssemblyFileNamesToScanForExtensions()
        {
            return base.GetAssemblyFileNamesToScanForExtensions();
        }
    }
}
