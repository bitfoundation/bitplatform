using Bit.Test;
using Bit.Test.Server;
using Bit.Http.Contracts;
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
                Token token = await testEnvironment.Server.LoginWithCredentials("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                using (WebDriver driver = testEnvironment.Server.BuildWebDriver(new WebDriverOptions { Token = token }))
                {
                    await driver.NavigateToRoute("angular-translate-page");

                    await driver.ExecuteTest("Bit.Tests.Implementations.Tests.AngularTranslateTests.testAngularTranslateViewModel");
                }
            }
        }

        [TestMethod]
        [TestCategory("HtmlClient"), TestCategory("Angular")]
        public virtual async Task TestDateTimeFiltersInEnUsCulture()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment(new TestEnvironmentArgs { UseRealServer = true }))
            {
                Token token = await testEnvironment.Server.LoginWithCredentials("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                using (WebDriver driver = testEnvironment.Server.BuildWebDriver(new WebDriverOptions { Token = token }))
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
                Token token = await testEnvironment.Server.LoginWithCredentials("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                using (WebDriver driver = testEnvironment.Server.BuildWebDriver(new WebDriverOptions { Token = token }))
                {
                    await driver.NavigateToRoute("date-time-service-page");

                    Assert.AreEqual("۱۳۹۴/۱۱/۱۲", driver.GetElementById("date").Text);

                    Assert.AreEqual("۱۲ بهمن ۱۳۹۴, ۱۰:۱۰ ق ظ", driver.GetElementById("dateTime").Text);
                }
            }
        }
    }
}