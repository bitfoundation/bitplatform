using FakeItEasy;
using Foundation.Api.ApiControllers;
using Foundation.Model.Dtos;
using Foundation.Test.Core.Implementations;
using IdentityModel.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium.Remote;
using System.Linq;

namespace Foundation.Test.HtmlClient.ViewModel.Implementation
{
    [TestClass]
    public class LoggerTests
    {
        [TestMethod]
        [TestCategory("HtmlClient"), TestCategory("Logging")]
        [Ignore]
        public virtual void LogException()
        {
            using (TestEnvironment testEnvironment = new TestEnvironment(new TestEnvironmentArgs { UseRealServer = true }))
            {
                TokenResponse token = testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientName: "TestResOwner");

                try
                {
                    using (RemoteWebDriver driver = testEnvironment.Server.GetWebDriver(new RemoteWebDriverOptions { Token = token }))
                    {
                        driver.ExecuteTest("LoggerTests.logException");
                    }

                    Assert.Fail();
                }
                catch
                {
                    ClientsLogsController clientsLogsController = TestDependencyManager.CurrentTestDependencyManager
                        .Objects.OfType<ClientsLogsController>().Single();

                    A.CallTo(() => clientsLogsController.Create(A<ClientLogDto>.That.Matches(cl => cl.ErrorName == "TypeError")))
                        .MustHaveHappened(Repeated.Exactly.Once);
                }
            }
        }
    }
}