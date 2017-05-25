using System.Linq;
using Bit.Core.Contracts;
using Bit.Test;
using Bit.Test.Core.Implementations;
using Bit.Test.Server;
using IdentityModel.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Remote;

namespace Bit.Tests.HtmlClient.ViewModel.Implementation
{
    [TestClass]
    public class SignalRTests
    {
        [TestMethod]
        [TestCategory("HtmlClient"), TestCategory("SignalR")]
        public virtual void TestSignalRConnection()
        {
            using (TestEnvironment testEnvironment = new TestEnvironment(new TestEnvironmentArgs { UseRealServer = true }))
            {
                TokenResponse token = testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientName: "TestResOwner");

                using (RemoteWebDriver driver = testEnvironment.Server.GetWebDriver(new RemoteWebDriverOptions { Token = token }))
                {
                    driver.ExecuteTest("testSignalRConnection");
                }

                ILogger logger = TestDependencyManager.CurrentTestDependencyManager
                    .Objects.OfType<ILogger>().Last();

                Assert.IsTrue(logger.LogData.Any(ld => ld.Key == nameof(IRequestInformationProvider.RequestUri) && ((string)ld.Value).Contains("signalr/start")));
            }
        }

        [TestMethod]
        [TestCategory("HtmlClient"), TestCategory("SignalR")]
        public virtual void TestSignalRNoConnectionShouldBeMade()
        {
            using (TestEnvironment testEnvironment = new TestEnvironment(new TestEnvironmentArgs { UseRealServer = true }))
            {
                TokenResponse token = testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientName: "TestResOwner");

                using (testEnvironment.Server.GetWebDriver(new RemoteWebDriverOptions { Token = token }))
                {

                }

                Assert.IsFalse(TestDependencyManager.CurrentTestDependencyManager
                .Objects.OfType<ILogger>()
                .Any(logger => logger.LogData.Any(ld => ld.Key == nameof(IRequestInformationProvider.RequestUri) && ((string)ld.Value).Contains("SignalR/start"))));
            }
        }
    }
}
