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
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment(new TestEnvironmentArgs { UseRealServer = false , UseAspNetCore = true }))
            {
                HttpResponseMessage response = await testEnvironment.Server.BuildHttpClient()
                    .GetAsync("api-core/People/GetData");

                response.EnsureSuccessStatusCode();

                Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            }
        }
    }
}
