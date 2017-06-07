using System.Collections.Generic;
using Bit.Test;

namespace Bit.Tests
{
    public class TestFakeItEasyLoadNothingBootstrapper : FakeItEasyLoadNothingBootstrapperBase
    {
        public override IEnumerable<string> GetAssemblyFileNamesToScanForExtensions()
        {
            return base.GetAssemblyFileNamesToScanForExtensions();
        }
    }
}
