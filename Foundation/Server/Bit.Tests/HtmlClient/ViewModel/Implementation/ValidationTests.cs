using Bit.Test;
using Bit.Test.Server;
using IdentityModel.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Remote;

namespace Bit.Tests.HtmlClient.ViewModel.Implementation
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
                TokenResponse token = testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientName: "TestResOwner");

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
                TokenResponse token = testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientName: "TestResOwner");

                using (RemoteWebDriver driver = testEnvironment.Server.GetWebDriver(new RemoteWebDriverOptions { Token = token }))
                {
                    driver.NavigateToRoute("form-validation-page");

                    driver.ExecuteTest("Foundation.Test.Implementations.Tests.ValidationTests.testValidationFormViewModelWithInValidBehavior");
                }
            }
        }
    }
}
