using Foundation.Test;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Foundation.AspNetCore.Test.Api
{
    [TestClass]
    public class WebApiCoreTests
    {
        [TestMethod]
        public virtual async Task WebApiCoreControllerShouldReturnOkStatusCode()
        {
            using (AspNetCoreTestEnvironment testEnvironment = new AspNetCoreTestEnvironment(new TestEnvironmentArgs { UseRealServer = false }))
            {
                HttpResponseMessage response = await testEnvironment.Server.GetHttpClient()
                    .GetAsync("api-core/People/GetData");

                response.EnsureSuccessStatusCode();

                Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            }
        }
    }
}
