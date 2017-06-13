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
    public class ValidationTests
    {
        [TestMethod]
        [TestCategory("HtmlClient"), TestCategory("Validation")]
        public virtual async Task TestValidationFormViewModelWithValidBehavior()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment(new TestEnvironmentArgs { UseRealServer = true }))
            {
                TokenResponse token = await testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                using (RemoteWebDriver driver = testEnvironment.Server.GetWebDriver(new RemoteWebDriverOptions { Token = token }))
                {
                    driver.NavigateToRoute("form-validation-page");

                    driver.ExecuteTest("Bit.Tests.Implementations.Tests.ValidationTests.testValidationFormViewModelWithValidBehavior");
                }
            }
        }

        [TestMethod]
        [TestCategory("HtmlClient"), TestCategory("Validation")]
        public virtual async Task TestValidationFormViewModelWithInValidBehavior()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment(new TestEnvironmentArgs { UseRealServer = true }))
            {
                TokenResponse token = await testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                using (RemoteWebDriver driver = testEnvironment.Server.GetWebDriver(new RemoteWebDriverOptions { Token = token }))
                {
                    driver.NavigateToRoute("form-validation-page");

                    driver.ExecuteTest("Bit.Tests.Implementations.Tests.ValidationTests.testValidationFormViewModelWithInValidBehavior");
                }
            }
        }
    }
}
