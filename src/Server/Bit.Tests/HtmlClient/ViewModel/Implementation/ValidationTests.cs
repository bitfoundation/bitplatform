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
    public class ValidationTests
    {
        [TestMethod]
        [TestCategory("HtmlClient"), TestCategory("Validation")]
        public virtual async Task TestValidationViewModelWithValidBehavior()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment(new TestEnvironmentArgs { UseRealServer = true }))
            {
                Token token = await testEnvironment.Server.LoginWithCredentials("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                using (WebDriver driver = testEnvironment.Server.BuildWebDriver(new WebDriverOptions { Token = token }))
                {
                    await driver.NavigateToRoute("form-validation-page");

                    await driver.ExecuteTest("Bit.Tests.Implementations.Tests.ValidationTests.testValidationViewModelWithValidBehavior");
                }
            }
        }

        [TestMethod]
        [TestCategory("HtmlClient"), TestCategory("Validation")]
        public virtual async Task TestValidationViewModelWithInValidBehavior()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment(new TestEnvironmentArgs { UseRealServer = true }))
            {
                Token token = await testEnvironment.Server.LoginWithCredentials("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                using (WebDriver driver = testEnvironment.Server.BuildWebDriver(new WebDriverOptions { Token = token }))
                {
                    await driver.NavigateToRoute("form-validation-page");

                    await driver.ExecuteTest("Bit.Tests.Implementations.Tests.ValidationTests.testValidationViewModelWithInValidBehavior");
                }
            }
        }
    }
}
