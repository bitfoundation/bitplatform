using Bit.Test;
using Bit.Test.Server;
using Bit.Http.Contracts;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Remote;
using Simple.OData.Client;
using System.Net.Http;
using System.Threading.Tasks;

namespace Bit.Tests.Api.ApiControllers.Tests
{
    [TestClass]
    public class SimpleDtoControllerTests
    {
        [TestMethod]
        [TestCategory("OData")]
        public virtual async Task SimpleODataInCSharpClientTest()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment())
            {
                Token token = await testEnvironment.Server.LoginWithCredentials("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                IODataClient client = testEnvironment.BuildTestODataClient(token: token);

                Assert.AreEqual(3, await client.Simple().Sum(1, 2).ExecuteAsScalarAsync<int>());
            }

            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment())
            {
                Token token = await testEnvironment.Server.LoginWithCredentials("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                HttpClient client = testEnvironment.Server.BuildHttpClient(token: token);

                Assert.AreEqual(3, await client.Simple().Sum(1, 2));
            }
        }

        [TestMethod]
        [TestCategory("HtmlClient"), TestCategory("JayDataContextOData")]
        public virtual async Task SimpleODataInTSClientTest()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment(new TestEnvironmentArgs { UseRealServer = true }))
            {
                Token token = await testEnvironment.Server.LoginWithCredentials("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                using (WebDriver driver = testEnvironment.Server.BuildWebDriver(new WebDriverOptions { Token = token }))
                {
                    await driver.ExecuteTest(@"simpleDtoControllerTest");
                }
            }
        }
    }
}
