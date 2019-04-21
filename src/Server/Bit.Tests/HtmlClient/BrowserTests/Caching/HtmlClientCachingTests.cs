using Bit.Core.Contracts;
using Bit.Owin.Contracts;
using Bit.Test;
using Bit.Test.Implementations;
using Bit.Test.Server;
using IdentityModel.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium.Remote;
using System.Linq;
using System.Threading.Tasks;

namespace Bit.Tests.HtmlClient.BrowserTests.Caching
{
    [TestClass]
    public class HtmlClientCachingTests
    {
        [TestMethod]
        [TestCategory("HtmlClient"), TestCategory("Caching")]
        public virtual async Task ResourceLikeIndexPageWhichInNotCachableMustBeRertivedEverytimeByHtmlClient()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment(new TestEnvironmentArgs { UseRealServer = true }))
            {
                TokenResponse token = await testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                using (RemoteWebDriver driver = testEnvironment.Server.BuildWebDriver(new RemoteWebDriverOptions { Token = token }))
                {
                    driver.Navigate().Refresh();
                }

                Assert.AreNotEqual(1, TestDependencyManager.CurrentTestDependencyManager.Objects.OfType<IHtmlPageProvider>().Count());
            }
        }

        [TestMethod]
        [TestCategory("HtmlClient"), TestCategory("Caching")]
        public virtual async Task ResourceLikeMetadataWhichAreCachableMustNotBeRetrievedEverytimeByHtmlClient()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment(new TestEnvironmentArgs { UseRealServer = true, ActiveAppEnvironmentCustomizer = activeAppEnv => activeAppEnv.DebugMode = false }))
            {
                TokenResponse token = await testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                using (RemoteWebDriver driver = testEnvironment.Server.BuildWebDriver(new RemoteWebDriverOptions { Token = token }))
                {
                    await Task.Delay(1000);

                    driver.Navigate().Refresh();
                }

                Assert.AreEqual(1, TestDependencyManager.CurrentTestDependencyManager.Objects.OfType<ILogger>()
                                    .Count(logger => logger.LogData.Any(ld => ld.Key == nameof(IRequestInformationProvider.DisplayUrl) && ((string)ld.Value).EndsWith(@"Metadata/V1"))));
            }
        }
    }
}
