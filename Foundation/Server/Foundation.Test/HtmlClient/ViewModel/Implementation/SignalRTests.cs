using System.Linq;
using Foundation.Core.Contracts;
using Foundation.Test.Core.Implementations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium.Remote;

namespace Foundation.Test.HtmlClient.ViewModel.Implementation
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
                OAuthToken token = testEnvironment.Server.Login("ValidUserName", "ValidPassword");

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
                OAuthToken token = testEnvironment.Server.Login("ValidUserName", "ValidPassword");

                using (RemoteWebDriver driver = testEnvironment.Server.GetWebDriver(new RemoteWebDriverOptions { Token = token }))
                {

                }

                Assert.IsFalse(TestDependencyManager.CurrentTestDependencyManager
                .Objects.OfType<ILogger>()
                .Any(logger => logger.LogData.Any(ld => ld.Key == nameof(IRequestInformationProvider.RequestUri) && ((string)ld.Value).Contains("SignalR/start"))));
            }
        }
    }
}
