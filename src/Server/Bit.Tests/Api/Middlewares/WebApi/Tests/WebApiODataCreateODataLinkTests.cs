using Bit.WebApi.Contracts;
using Bit.Http.Contracts;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Simple.OData.Client;
using System.Net.Http;
using System.Threading.Tasks;

namespace Bit.Tests.Api.Middlewares.WebApi.Tests
{
    [TestClass]
    public class WebApiODataCreateODataLinkTests
    {
        [TestMethod]
        [TestCategory("OData"), TestCategory("WebApi")]
        public virtual async Task CreateODataLinkAndCallItTest()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment())
            {
                Token token = await testEnvironment.Server.LoginWithCredentials("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                IODataClient client = testEnvironment.BuildTestODataClient(token: token);

                string newUrl = await client.TestModels()
                    .CreateODataLinkSample()
                    .ExecuteAsScalarAsync<string>();

                int sumResult = (await (await testEnvironment.Server.BuildHttpClient(token)
                    .GetAsync(newUrl))
                    .EnsureSuccessStatusCode()
                    .Content
                    .ReadAsAsync<ODataResponse<int>>()).Value;

                Assert.AreEqual(3, sumResult);
            }
        }
    }
}
