using System;
using System.Collections.Generic;

namespace Bit.Test
{
    public class FakeItEasyLoadNothingBootstrapperBase : FakeItEasy.IBootstrapper
    {
        public virtual IEnumerable<string> GetAssemblyFileNamesToScanForExtensions()
        {
            return Array.Empty<string>();
        }
    }
}
