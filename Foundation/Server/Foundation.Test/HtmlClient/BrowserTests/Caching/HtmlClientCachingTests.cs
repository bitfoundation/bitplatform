using System.Linq;
using Foundation.Api.Contracts;
using Foundation.Core.Contracts;
using Foundation.Test.Core.Implementations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium.Remote;

namespace Foundation.Test.HtmlClient.BrowserTests.Caching
{
    [TestClass]
    public class HtmlClientCachingTests
    {
        [TestMethod]
        [TestCategory("HtmlClient"), TestCategory("Caching")]
        public virtual void ResourceLikeDefaultPageWhichInNotCachableMustBeRertivedEverytimeByHtmlClient()
        {
            using (TestEnvironment testEnvironment = new TestEnvironment(new TestEnvironmentArgs { UseRealServer = true }))
            {
                OAuthToken token = testEnvironment.Server.Login("ValidUserName", "ValidPassword");

                using (RemoteWebDriver driver = testEnvironment.Server.GetWebDriver(new RemoteWebDriverOptions { Token = token }))
                {
                    driver.Navigate().Refresh();
                }

                Assert.AreNotEqual(1, TestDependencyManager.CurrentTestDependencyManager.Objects.OfType<IDefaultHtmlPageProvider>().Count());
            }
        }

        [TestMethod]
        [TestCategory("HtmlClient"), TestCategory("Caching")]
        public virtual void ResourceLikeMetadataWhichAreCachableMustNotBeRertivedEverytimeByHtmlClient()
        {
            using (TestEnvironment testEnvironment = new TestEnvironment(new TestEnvironmentArgs { UseRealServer = true, ActiveAppEnvironmentCustomizer = activeAppEnv => activeAppEnv.DebugMode = false }))
            {
                OAuthToken token = testEnvironment.Server.Login("ValidUserName", "ValidPassword");

                using (RemoteWebDriver driver = testEnvironment.Server.GetWebDriver(new RemoteWebDriverOptions { Token = token }))
                {
                    driver.Navigate().Refresh();
                }

                Assert.AreEqual(1, TestDependencyManager.CurrentTestDependencyManager.Objects.OfType<ILogger>()
                    .Count(logger => logger.LogData.Any(ld => ld.Key == nameof(IRequestInformationProvider.RequestUri) && ((string)ld.Value).EndsWith(@"Metadata/V1"))));
            }
        }
    }
}
