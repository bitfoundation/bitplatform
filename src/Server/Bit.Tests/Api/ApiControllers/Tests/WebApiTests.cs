using IdentityModel.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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
    }

}
