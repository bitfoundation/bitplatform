using System.Linq;
using Bit.Core.Contracts;
using Bit.Owin.Contracts;
using Bit.Test;
using Bit.Test.Core.Implementations;
using Bit.Test.Server;
using IdentityModel.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium.Remote;

namespace Bit.Tests.HtmlClient.BrowserTests.Caching
{
    [TestClass]
    public class HtmlClientCachingTests
    {
        [TestMethod]
        [TestCategory("HtmlClient"), TestCategory("Caching")]
        public virtual void ResourceLikeDefaultPageWhichInNotCachableMustBeRertivedEverytimeByHtmlClient()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment(new TestEnvironmentArgs { UseRealServer = true }))
            {
                TokenResponse token = testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientName: "TestResOwner");

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
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment(new TestEnvironmentArgs { UseRealServer = true, ActiveAppEnvironmentCustomizer = activeAppEnv => activeAppEnv.DebugMode = false }))
            {
                TokenResponse token = testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientName: "TestResOwner");

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
