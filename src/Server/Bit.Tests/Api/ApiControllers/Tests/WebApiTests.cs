using IdentityModel.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Bit.Tests.Api.ApiControllers.Tests
{
    [TestClass]
    public class WebApiTests
    {
        [TestMethod]
        [TestCategory("WebApi")]
        public virtual async Task WebApiShouldReturnOk()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment())
            {
                TokenResponse token = await testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                HttpClient client = testEnvironment.Server.BuildHttpClient(token: token);

                HttpResponseMessage response = await client.GetAsync("api/customers/1/orders");

                response.EnsureSuccessStatusCode();

                Assert.AreEqual(1, await response.Content.ReadAsAsync<int>());
            }
        }

        [TestMethod]
        [TestCategory("WebApi")]
        public virtual async Task WebApiShouldReturnSumOfTwoNumbersForRefitClient()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment())
            {
                TokenResponse token = await testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                ICustomersService customersService = testEnvironment.Server.BuildRefitClient<ICustomersService>(token: token);

                int response = await customersService.Sum(1, 2, CancellationToken.None);

                Assert.AreEqual(3, response);
            }
        }

        [TestMethod]
        [TestCategory("WebApi")]
        public virtual async Task WebApiVersioningTest()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment())
            {
                TokenResponse token = await testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                HttpClient httpClient = testEnvironment.Server.BuildHttpClient(token);

                Assert.AreEqual(1, await (await httpClient.GetAsync("api/v1/version-test/get-value")).Content.ReadAsAsync<int>());
                Assert.AreEqual(2, await (await httpClient.GetAsync("api/v2/version-test/get-value")).Content.ReadAsAsync<int>());
                try
                {
                    (await httpClient.GetAsync("api/v3/version-test/get-value")).EnsureSuccessStatusCode();
                    Assert.Fail();
                }
                catch (Exception ex) when (ex.Message == "Response status code does not indicate success: 400 (UnknownError:Bad Request).")
                {

                }
                Assert.AreEqual(2, await (await httpClient.GetAsync("api/customers/sum/1/1?api-version=1.0")).Content.ReadAsAsync<int>());
            }
        }

        [TestMethod]
        [TestCategory("WebApi")]
        public virtual async Task TestWebApiDefaultRouting()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment())
            {
                TokenResponse token = await testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                HttpClient httpClient = testEnvironment.Server.BuildHttpClient(token);

                (await httpClient.GetAsync("api/values")).EnsureSuccessStatusCode();
            }
        }

        [TestMethod]
        [TestCategory("WebApi")]
        public virtual async Task GetSwaggerShouldWork()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment())
            {
                TokenResponse token = await testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                HttpClient client = testEnvironment.Server.BuildHttpClient(token: token);

                HttpResponseMessage response = await client.GetAsync("api/v1/swagger");

                response.EnsureSuccessStatusCode();
            }
        }
    }
}
