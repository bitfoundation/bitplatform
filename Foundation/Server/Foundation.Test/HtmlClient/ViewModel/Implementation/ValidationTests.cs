using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium.Remote;

namespace Foundation.Test.HtmlClient.ViewModel.Implementation
{
    [TestClass]
    public class ValidationTests
    {
        [TestMethod]
        [TestCategory("HtmlClient"), TestCategory("Validation")]
        public virtual void TestValidationFormViewModelWithValidBehavior()
        {
            using (TestEnvironment testEnvironment = new TestEnvironment(new TestEnvironmentArgs { UseRealServer = true }))
            {
                OAuthToken token = testEnvironment.Server.Login("ValidUserName", "ValidPassword");

                using (RemoteWebDriver driver = testEnvironment.Server.GetWebDriver(new RemoteWebDriverOptions { Token = token }))
                {
                    driver.NavigateToRoute("form-validation-page");

                    driver.ExecuteTest("Foundation.Test.Implementations.Tests.ValidationTests.testValidationFormViewModelWithValidBehavior");
                }
            }
        }

        [TestMethod]
        [TestCategory("HtmlClient"), TestCategory("Validation")]
        public virtual void TestValidationFormViewModelWithInValidBehavior()
        {
            using (TestEnvironment testEnvironment = new TestEnvironment(new TestEnvironmentArgs { UseRealServer = true }))
            {
                OAuthToken token = testEnvironment.Server.Login("ValidUserName", "ValidPassword");

                using (RemoteWebDriver driver = testEnvironment.Server.GetWebDriver(new RemoteWebDriverOptions { Token = token }))
                {
                    driver.NavigateToRoute("form-validation-page");

                    driver.ExecuteTest("Foundation.Test.Implementations.Tests.ValidationTests.testValidationFormViewModelWithInValidBehavior");
                }
            }
        }
    }
}
