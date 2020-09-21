using Bit.Test;
using IdentityModel.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Bit.Tests.Api.ApiControllers.Tests
{
    [TestClass]
    public class WebApiCoreTests
    {
        [TestMethod]
        [TestCategory("WebApiCore")]
        public virtual async Task WebApiCoreControllerShouldReturnOkStatusCode()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment(new TestEnvironmentArgs { UseRealServer = false }))
            {
                TokenResponse token = await testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                HttpClient client = testEnvironment.Server.BuildHttpClient(token: token);

                HttpResponseMessage response = await client
                    .GetAsync("api-core/People/GetData");

                response.EnsureSuccessStatusCode();

                Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            }
        }
    }
}
