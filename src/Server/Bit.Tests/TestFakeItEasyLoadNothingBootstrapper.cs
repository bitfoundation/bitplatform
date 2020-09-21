using Bit.Test;
using System.Collections.Generic;

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
