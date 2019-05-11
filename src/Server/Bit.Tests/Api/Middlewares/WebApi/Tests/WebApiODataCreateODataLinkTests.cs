using Bit.WebApi.Contracts;
using IdentityModel.Client;
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
                TokenResponse token = await testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                IODataClient client = testEnvironment.Server.BuildODataClient(token: token);

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
