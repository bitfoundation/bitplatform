using System.Collections.Generic;

namespace Foundation.Test
{
    public class FakeItEasyLoadNothingBootstrapperBase : FakeItEasy.IBootstrapper
    {
        public virtual IEnumerable<string> GetAssemblyFileNamesToScanForExtensions()
        {
            return new string[] { };
        }
    }
}
