using IdentityModel.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Bit.Tests.Api.Middlewares.WebApi.Tests
{
    [TestClass]
    public class ODataMetadataTests
    {
        [TestMethod]
        [TestCategory("WebApi"), TestCategory("OData")]
        public async Task AllODataServicesMustProvideTheirOwnMetadata()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment())
            {
                TokenResponse token = await testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                string[] odataServicesRoutes = { "Bit", "Test" };

                foreach (string odataServiceRoute in odataServicesRoutes)
                {
                    HttpResponseMessage getMetadataResponse = await testEnvironment.Server.BuildHttpClient(token)
                            .GetAsync($"/odata/{odataServiceRoute}/$metadata");

                    Assert.AreEqual(HttpStatusCode.OK, getMetadataResponse.StatusCode);

                    Assert.AreEqual("application/xml", getMetadataResponse.Content.Headers.ContentType.MediaType);
                }
            }
        }
    }
}
