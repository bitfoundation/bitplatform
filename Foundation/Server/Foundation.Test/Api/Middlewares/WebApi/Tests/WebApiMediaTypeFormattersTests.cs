using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Foundation.Test.Api.Middlewares.WebApi.Tests
{
    [TestClass]
    public class WebApiMediaTypeFormattersTests
    {
        [TestMethod]
        [TestCategory("WebApi")]
        public virtual async Task MediaTypeFormattersShouldReturnMinimalStreamedODataJsonWhenNoContentTypeIsDeclaredInRequest()
        {
            using (TestEnvironment testEnvironment = new TestEnvironment())
            {
                OAuthToken token = testEnvironment.Server.Login("ValidUserName", "ValidPassword");

                HttpResponseMessage getTestModelsResponse = await testEnvironment.Server.GetHttpClient(token)
                    .GetAsync("/odata/Test/TestModels");

                Assert.AreEqual("application/json; odata.metadata=minimal; odata.streaming=true", getTestModelsResponse.Content.Headers.ContentType.ToString());
            }
        }

        [TestMethod]
        [TestCategory("WebApi")]
        public virtual async Task MediaTypeFormattersShouldReturnMinimalODataJsonWhenJsonContentTypeIsDeclaredFirstInRequest()
        {
            using (TestEnvironment testEnvironment = new TestEnvironment())
            {
                OAuthToken token = testEnvironment.Server.Login("ValidUserName", "ValidPassword");

                HttpResponseMessage getTestModelsResponse = await testEnvironment.Server.GetHttpClient(token)
                    .AddHeader("Accept", "application/json, text/javascript, */*; q=0.01")
                    .GetAsync("/odata/Test/TestModels");

                Assert.AreEqual("application/json; odata.metadata=minimal", getTestModelsResponse.Content.Headers.ContentType.ToString());
            }
        }

        [TestMethod]
        [TestCategory("WebApi")]
        public virtual async Task MediaTypeFormattersShouldReturnMinimalODataJsonWhenStarContentTypeIsDeclaredInRequest()
        {
            using (TestEnvironment testEnvironment = new TestEnvironment())
            {
                OAuthToken token = testEnvironment.Server.Login("ValidUserName", "ValidPassword");

                HttpResponseMessage getTestModelsResponse = await testEnvironment.Server.GetHttpClient(token)
                    .AddHeader("Accept", "*/*; q=0.01")
                    .GetAsync("/odata/Test/TestModels");

                Assert.AreEqual("application/json; odata.metadata=minimal", getTestModelsResponse.Content.Headers.ContentType.ToString());
            }
        }

        [TestMethod]
        [TestCategory("WebApi")]
        public virtual async Task MediaTypeFormattersShouldReturnMinimalStreamedODataJsonWhenInvalidContentTypeIsDeclaredInRequest()
        {
            using (TestEnvironment testEnvironment = new TestEnvironment())
            {
                OAuthToken token = testEnvironment.Server.Login("ValidUserName", "ValidPassword");

                HttpResponseMessage getTestModelsResponse = await testEnvironment.Server.GetHttpClient(token)
                    .AddHeader("Accept", "text/html; q=0.01")
                    .GetAsync("/odata/Test/TestModels");

                Assert.AreEqual("application/json; odata.metadata=minimal; odata.streaming=true", getTestModelsResponse.Content.Headers.ContentType.ToString());
            }
        }
    }
}
