using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Tests.Api
{
    [TestClass]
    public class TestServerFeaturesTests
    {
        [TestMethod]
        public async Task GetServerAddressShouldWorkWhileUsingFakeTestServer()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment())
            {
                var hostUri = testEnvironment.DependencyResolver.Resolve<IServer>()
                    .GetHostUri();

                Assert.AreEqual(new Uri("http://localhost"), hostUri);
            }
        }
    }
}
