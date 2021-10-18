using Bit.Core.Contracts;
using Bit.Owin.Contracts;
using Bit.Test;
using Bit.Test.Server;
using Bit.Http.Contracts;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium.Remote;
using System.Linq;
using System.Threading.Tasks;
using OpenQA.Selenium;

namespace Bit.Tests.HtmlClient.BrowserTests.Caching
{
    [TestClass]
    public class HtmlClientCachingTests
    {
        [TestMethod]
        [TestCategory("HtmlClient"), TestCategory("Caching")]
        public virtual async Task ResourceLikeIndexPageWhichInNotCachableMustBeRetrievedEverytimeByHtmlClient()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment(new TestEnvironmentArgs { UseRealServer = true }))
            {
                Token token = await testEnvironment.Server.LoginWithCredentials("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                using (WebDriver driver = testEnvironment.Server.BuildWebDriver(new WebDriverOptions { Token = token }))
                {
                    driver.Navigate().Refresh();
                }

                Assert.AreNotEqual(1, testEnvironment.GetObjects<IHtmlPageProvider>().Count());
            }
        }

        [TestMethod]
        [TestCategory("HtmlClient"), TestCategory("Caching")]
        public virtual async Task ResourceLikeMetadataWhichAreCachableMustNotBeRetrievedEverytimeByHtmlClient()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment(new TestEnvironmentArgs { UseRealServer = true, ActiveAppEnvironmentCustomizer = activeAppEnv => activeAppEnv.DebugMode = false }))
            {
                Token token = await testEnvironment.Server.LoginWithCredentials("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                using (WebDriver driver = testEnvironment.Server.BuildWebDriver(new WebDriverOptions { Token = token }))
                {
                    await Task.Delay(1000);

                    driver.Navigate().Refresh();
                }

                Assert.AreEqual(1, testEnvironment.GetObjects<ILogger>()
                                    .Count(logger => logger.LogData.Any(ld => ld.Key == nameof(IRequestInformationProvider.DisplayUrl) && ((string)ld.Value).EndsWith(@"Metadata/V1"))));
            }
        }
    }
}
