using Foundation.Test;
using System.Collections.Generic;

namespace IdentityServer.Test
{
    public class FakeItEasyLoadNothingBootstrapper : FakeItEasyLoadNothingBootstrapperBase, FakeItEasy.IBootstrapper
    {
        public FakeItEasyLoadNothingBootstrapper()
        {

        }

        public override IEnumerable<string> GetAssemblyFileNamesToScanForExtensions()
        {
            return base.GetAssemblyFileNamesToScanForExtensions();
        }
    }
}
