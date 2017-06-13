using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using IdentityModel.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Tests.Api.Middlewares.WebApi.Tests
{
    [TestClass]
    public class WebApiODataSecurityTests
    {
        [TestMethod]
        [TestCategory("WebApi"), TestCategory("Security")]
        public async Task LoggedInUsersMustHaveAccessToODataWebApis()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment())
            {
                TokenResponse token = await testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                HttpResponseMessage getMetadataResponse = await testEnvironment.Server.GetHttpClient(token)
                        .GetAsync("/odata/Bit/$metadata");

                Assert.AreEqual(HttpStatusCode.OK, getMetadataResponse.StatusCode);
            }
        }

        [TestMethod]
        [TestCategory("WebApi"), TestCategory("Security")]
        public async Task WebApiResponsesMustHaveSecurityHeaders()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment())
            {
                TokenResponse token = await testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                HttpResponseMessage getMetadataResponse = await testEnvironment.Server.GetHttpClient(token)
                        .GetAsync("/odata/Bit/$metadata");

                Assert.AreEqual(true, getMetadataResponse.Headers.Contains("X-Content-Type-Options"));
            }
        }

        [TestMethod]
        [TestCategory("WebApi"), TestCategory("Security")]
        public async Task NotLoggedInUsersMustHaveAccessToMetadataAnyway()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment())
            {
                HttpResponseMessage getMetadataResponse = await testEnvironment.Server.GetHttpClient()
                        .GetAsync("/odata/Test/$metadata");

                Assert.AreEqual(HttpStatusCode.OK, getMetadataResponse.StatusCode);
            }
        }

        [TestMethod]
        [TestCategory("WebApi"), TestCategory("Security")]
        public async Task NotLoggedInUsersMustNotHaveAccessToProtectedResources()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment())
            {
                HttpResponseMessage getMetadataResponse = await testEnvironment.Server.GetHttpClient()
                        .GetAsync("/odata/Test/ValidationSamples/");

                Assert.AreEqual(HttpStatusCode.Unauthorized, getMetadataResponse.StatusCode);
            }
        }

        [TestMethod]
        [TestCategory("WebApi"), TestCategory("Security")]
        public async Task NotLoggedInUsersMustHaveAccessToUnProtectedWebApis()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment())
            {
                HttpResponseMessage getTestModels = await testEnvironment.Server.GetHttpClient()
                        .GetAsync("/odata/Test/TestModels");

                Assert.AreEqual(HttpStatusCode.OK, getTestModels.StatusCode);
            }
        }
    }
}
