using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium.Remote;

namespace Foundation.Test.HtmlClient.ViewModel.Implementation
{
    [TestClass]
    public class LocalizitionTests
    {
        [TestMethod]
        [TestCategory("HtmlClient"), TestCategory("Angular")]
        public virtual void TestAngularTranslateFormViewModel()
        {
            using (TestEnvironment testEnvironment = new TestEnvironment(new TestEnvironmentArgs { UseRealServer = true }))
            {
                OAuthToken token = testEnvironment.Server.Login("ValidUserName", "ValidPassword");

                using (RemoteWebDriver driver = testEnvironment.Server.GetWebDriver(new RemoteWebDriverOptions { Token = token }))
                {
                    driver.NavigateToRoute("angular-translate-page");

                    driver.ExecuteTest("Foundation.Test.Implementations.Tests.AngularTranslateTests.testAngularTransalateFormViewModel");
                }
            }
        }

        [TestMethod]
        [TestCategory("HtmlClient"), TestCategory("Angular")]
        public virtual void TestDateTimeFiltersInEnUsCulture()
        {
            using (TestEnvironment testEnvironment = new TestEnvironment(new TestEnvironmentArgs { UseRealServer = true }))
            {
                OAuthToken token = testEnvironment.Server.Login("ValidUserName", "ValidPassword");

                using (RemoteWebDriver driver = testEnvironment.Server.GetWebDriver(new RemoteWebDriverOptions { Token = token }))
                {
                    driver.NavigateToRoute("date-time-service-page");

                    Assert.AreEqual("2016/01/02", driver.GetElementById("date").Text);

                    Assert.AreEqual("2016/01/02, 10:10 AM", driver.GetElementById("dateTime").Text);
                }
            }
        }

        [TestMethod]
        [TestCategory("HtmlClient"), TestCategory("Angular")]
        public virtual void TestDateTimeFiltersInFaIrCulture()
        {
            using (TestEnvironment testEnvironment = new TestEnvironment(new TestEnvironmentArgs
            {
                UseRealServer = true,
                ActiveAppEnvironmentCustomizer = appEnvironment =>
                {
                    appEnvironment.AppInfo.DefaultCulture = "FaIr";
                }
            }))
            {
                OAuthToken token = testEnvironment.Server.Login("ValidUserName", "ValidPassword");

                using (RemoteWebDriver driver = testEnvironment.Server.GetWebDriver(new RemoteWebDriverOptions { Token = token }))
                {
                    driver.NavigateToRoute("date-time-service-page");

                    Assert.AreEqual("۱۳۹۴/۱۱/۱۲", driver.GetElementById("date").Text);

                    Assert.AreEqual("۱۲ بهمن ۱۳۹۴, ۱۰:۱۰ ق ظ", driver.GetElementById("dateTime").Text);
                }
            }
        }
    }
}