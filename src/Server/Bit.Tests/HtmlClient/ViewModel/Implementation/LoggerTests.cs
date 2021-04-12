using Bit.Model.Dtos;
using Bit.OData.ODataControllers;
using Bit.Test;
using Bit.Test.Server;
using FakeItEasy;
using Bit.Http.Contracts;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Remote;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Bit.Tests.HtmlClient.ViewModel.Implementation
{
    [TestClass]
    public class LoggerTests
    {
        [TestMethod]
        [TestCategory("HtmlClient"), TestCategory("Logging")]
        public virtual async Task LogException()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment(new TestEnvironmentArgs { UseRealServer = true }))
            {
                Token token = await testEnvironment.Server.LoginWithCredentials("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                try
                {
                    using (RemoteWebDriver driver = testEnvironment.Server.BuildWebDriver(new RemoteWebDriverOptions { Token = token }))
                    {
                        try
                        {
                            await driver.ExecuteTest("LoggerTests.logException");
                        }
                        catch { }
                    }

                    Assert.Fail();
                }
                catch
                {
                    ClientsLogsController clientsLogsController = testEnvironment.GetObjects<ClientsLogsController>().Single();

                    A.CallTo(() => clientsLogsController.Create(A<ClientLogDto>.That.Matches(cl => cl.ErrorName == "TypeError")))
                        .MustHaveHappenedOnceExactly();
                }
            }
        }

        [TestMethod]
        [TestCategory("Logging")]
        public async Task XCorrelationIdTests()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment())
            {
                Token token = await testEnvironment.Server.LoginWithCredentials("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                HttpResponseMessage getIndexPageResponse = await testEnvironment.Server.BuildHttpClient(token)
                    .GetAsync("/");

                Assert.IsTrue(Guid.TryParse(getIndexPageResponse.Headers.GetValues("X-Correlation-ID").Single(), out Guid _));

                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "/");

                var xCorrelationId = Guid.NewGuid();

                request.Headers.Add("X-Correlation-ID", xCorrelationId.ToString());

                getIndexPageResponse = await testEnvironment.Server.BuildHttpClient(token)
                    .SendAsync(request);

                Assert.IsTrue(Guid.TryParse(getIndexPageResponse.Headers.GetValues("X-Correlation-ID").Single(), out Guid returnedXCorrelationId) && xCorrelationId == returnedXCorrelationId);
            }
        }
    }
}