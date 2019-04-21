using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Bit.Tests.Model.Dto;
using Bit.WebApi.Contracts;
using IdentityModel.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Tests.Api.Middlewares.WebApi.Tests
{
    [TestClass]
    public class WebApiMediaTypeFormattersTests
    {
        [TestMethod]
        [TestCategory("WebApi")]
        public virtual async Task MediaTypeFormattersShouldReturnMinimalStreamedODataJsonWhenNoContentTypeIsDeclaredInRequest()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment())
            {
                TokenResponse token = await testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                HttpResponseMessage getTestModelsResponse = await testEnvironment.Server.BuildHttpClient(token)
                    .GetAsync("/odata/Test/TestModels");

                Assert.AreEqual("application/json; odata.metadata=minimal; odata.streaming=true; charset=utf-8", getTestModelsResponse.Content.Headers.ContentType.ToString());
            }
        }

        [TestMethod]
        [TestCategory("WebApi")]
        public virtual async Task MediaTypeFormattersShouldReturnMinimalODataJsonWhenJsonContentTypeIsDeclaredFirstInRequest()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment())
            {
                TokenResponse token = await testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                HttpResponseMessage getTestModelsResponse = await testEnvironment.Server.BuildHttpClient(token)
                    .AddHeader("Accept", "application/json, text/javascript, */*; q=0.01")
                    .GetAsync("/odata/Test/TestModels");

                Assert.AreEqual("application/json; charset=utf-8; odata.metadata=minimal", getTestModelsResponse.Content.Headers.ContentType.ToString());
            }
        }

        [TestMethod]
        [TestCategory("WebApi")]
        public virtual async Task MediaTypeFormattersShouldReturnMinimalODataJsonWhenStarContentTypeIsDeclaredInRequest()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment())
            {
                TokenResponse token = await testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                HttpResponseMessage getTestModelsResponse = await testEnvironment.Server.BuildHttpClient(token)
                    .AddHeader("Accept", "*/*; q=0.01")
                    .GetAsync("/odata/Test/TestModels");

                Assert.AreEqual("application/json; charset=utf-8; odata.metadata=minimal", getTestModelsResponse.Content.Headers.ContentType.ToString());
            }
        }

        [TestMethod]
        [TestCategory("WebApi")]
        public virtual async Task MediaTypeFormattersShouldReturnMinimalStreamedODataJsonWhenInvalidContentTypeIsDeclaredInRequest()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment())
            {
                TokenResponse token = await testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                HttpResponseMessage getTestModelsResponse = await testEnvironment.Server.BuildHttpClient(token)
                    .AddHeader("Accept", "text/html; q=0.01")
                    .GetAsync("/odata/Test/TestModels");

                Assert.AreEqual("application/json; odata.metadata=minimal; odata.streaming=true; charset=utf-8", getTestModelsResponse.Content.Headers.ContentType.ToString());
            }
        }

        [TestMethod]
        [TestCategory("WebApi")]
        public virtual async Task ODataResponseTest()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment())
            {
                TokenResponse token = await testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                {
                    HttpResponseMessage getCustomersResponse = await testEnvironment.Server.BuildHttpClient(token)
                        .GetAsync("odata/v1/TestCustomers?$count=true");

                    var odataResponse = await getCustomersResponse.Content.ReadAsAsync<ODataResponse<TestCustomerDto[]>>();

                    Assert.AreEqual(1, odataResponse.TotalCount);
                    Assert.AreEqual("TestCustomer", odataResponse.Value.Single().Name);
                    Assert.AreEqual("http://localhost/odata/v1/$metadata#TestCustomers", odataResponse.Context);
                }

                {
                    HttpResponseMessage getCustomersResponse = await testEnvironment.Server.BuildHttpClient(token)
                        .GetAsync("odata/v1/TestCustomers");

                    var odataResponse = await getCustomersResponse.Content.ReadAsAsync<ODataResponse<TestCustomerDto[]>>();

                    Assert.AreEqual(null, odataResponse.TotalCount);
                    Assert.AreEqual("TestCustomer", odataResponse.Value.Single().Name);
                    Assert.AreEqual("http://localhost/odata/v1/$metadata#TestCustomers", odataResponse.Context);
                }
            }
        }
    }
}
