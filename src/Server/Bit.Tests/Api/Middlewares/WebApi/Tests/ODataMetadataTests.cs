using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using IdentityModel.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Tests.Api.Middlewares.WebApi.Tests
{
    [TestClass]
    public class ODataMetadataTests
    {
        [TestMethod]
        [TestCategory("WebApi"), TestCategory("OData")]
        public async Task AllEdmProvidersMustProvideTheirOwnMetadata()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment())
            {
                TokenResponse token = await testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientName: "TestResOwner");

                string[] edmModelProviders = new string[] { "Bit", "Test" };

                foreach (string edmModelProvider in edmModelProviders)
                {
                    HttpResponseMessage getMetadataResponse = await testEnvironment.Server.GetHttpClient(token)
                            .GetAsync($"/odata/{edmModelProvider}/$metadata");

                    Assert.AreEqual(HttpStatusCode.OK, getMetadataResponse.StatusCode);

                    Assert.AreEqual("application/xml", getMetadataResponse.Content.Headers.ContentType.MediaType);
                }
            }
        }
    }
}
