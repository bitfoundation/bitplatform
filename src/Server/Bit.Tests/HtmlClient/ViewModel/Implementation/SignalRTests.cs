using System.Linq;
using Bit.Core.Contracts;
using Bit.Test;
using Bit.Test.Implementations;
using Bit.Test.Server;
using IdentityModel.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Remote;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Bit.Tests.HtmlClient.ViewModel.Implementation
{
    [TestClass]
    public class SignalRTests
    {
        [TestMethod]
        [TestCategory("HtmlClient"), TestCategory("SignalR")]
        public virtual async Task TestSignalRConnection()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment(new TestEnvironmentArgs { UseRealServer = true }))
            {
                TokenResponse token = await testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                using (RemoteWebDriver driver = testEnvironment.Server.BuildWebDriver(new RemoteWebDriverOptions { Token = token }))
                {
                    await driver.ExecuteTest("testSignalRConnection");
                }

                IEnumerable<ILogger> loggers = TestDependencyManager.CurrentTestDependencyManager.Objects
                                    .OfType<ILogger>()
                                    .ToList();

                Assert.IsTrue(loggers.SelectMany(l => l.LogData).Any(ld => ld.Key == nameof(IRequestInformationProvider.DisplayUrl) && ((string)ld.Value).Contains("signalr/start")));
            }
        }

        [TestMethod]
        [TestCategory("HtmlClient"), TestCategory("SignalR")]
        public virtual async Task TestSignalRNoConnectionShouldBeMade()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment(new TestEnvironmentArgs { UseRealServer = true }))
            {
                TokenResponse token = await testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                using (testEnvironment.Server.BuildWebDriver(new RemoteWebDriverOptions { Token = token }))
                {

                }

                Assert.IsFalse(TestDependencyManager.CurrentTestDependencyManager
                .Objects.OfType<ILogger>()
                .Any(logger => logger.LogData.Any(ld => ld.Key == nameof(IRequestInformationProvider.DisplayUrl) && ((string)ld.Value).Contains("SignalR/start"))));
            }
        }
    }
}
