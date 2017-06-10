using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Bit.Test;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Tests.Api.ApiControllers.Tests
{
    [TestClass]
    public class WebApiCoreTests
    {
        [TestMethod]
        [TestCategory("WebApiCore")]
        public virtual async Task WebApiCoreControllerShouldReturnOkStatusCode()
        {
            using (BitOwinCoreTestEnvironment testEnvironment = new BitOwinCoreTestEnvironment(new TestEnvironmentArgs { UseRealServer = false }))
            {
                HttpResponseMessage response = await testEnvironment.Server.GetHttpClient()
                    .GetAsync("api-core/People/GetData");

                response.EnsureSuccessStatusCode();

                Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            }
        }
    }
}
