using Bit.Test;
using Bit.Test.Server;
using IdentityModel.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Remote;
using System.Threading.Tasks;

namespace Bit.Tests.HtmlClient.ViewModel.Implementation
{
    [TestClass]
    public class LocalizationTests
    {
        [TestMethod]
        [TestCategory("HtmlClient"), TestCategory("Angular")]
        public virtual async Task TestAngularTranslateViewModel()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment(new TestEnvironmentArgs { UseRealServer = true }))
            {
                TokenResponse token = await testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                using (RemoteWebDriver driver = testEnvironment.Server.GetWebDriver(new RemoteWebDriverOptions { Token = token }))
                {
                    await driver.NavigateToRoute("angular-translate-page");

                    await driver.ExecuteTest("Bit.Tests.Implementations.Tests.AngularTranslateTests.testAngularTransalateViewModel");
                }
            }
        }

        [TestMethod]
        [TestCategory("HtmlClient"), TestCategory("Angular")]
        public virtual async Task TestDateTimeFiltersInEnUsCulture()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment(new TestEnvironmentArgs { UseRealServer = true }))
            {
                TokenResponse token = await testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                using (RemoteWebDriver driver = testEnvironment.Server.GetWebDriver(new RemoteWebDriverOptions { Token = token }))
                {
                    await driver.NavigateToRoute("date-time-service-page");

                    Assert.AreEqual("2016/01/02", driver.GetElementById("date").Text);

                    Assert.AreEqual("2016/01/02, 10:10 AM", driver.GetElementById("dateTime").Text);
                }
            }
        }

        [TestMethod]
        [TestCategory("HtmlClient"), TestCategory("Angular")]
        public virtual async Task TestDateTimeFiltersInFaIrCulture()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment(new TestEnvironmentArgs
            {
                UseRealServer = true,
                ActiveAppEnvironmentCustomizer = appEnvironment =>
                {
                    appEnvironment.AppInfo.DefaultCulture = "FaIr";
                }
            }))
            {
                TokenResponse token = await testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                using (RemoteWebDriver driver = testEnvironment.Server.GetWebDriver(new RemoteWebDriverOptions { Token = token }))
                {
                    await driver.NavigateToRoute("date-time-service-page");

                    Assert.AreEqual("۱۳۹۴/۱۱/۱۲", driver.GetElementById("date").Text);

                    Assert.AreEqual("۱۲ بهمن ۱۳۹۴, ۱۰:۱۰ ق ظ", driver.GetElementById("dateTime").Text);
                }
            }
        }
    }
}