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
        public virtual async Task TestValidationViewModelWithValidBehavior()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment(new TestEnvironmentArgs { UseRealServer = true }))
            {
                TokenResponse token = await testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                using (RemoteWebDriver driver = testEnvironment.Server.BuildWebDriver(new RemoteWebDriverOptions { Token = token }))
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
                TokenResponse token = await testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                using (RemoteWebDriver driver = testEnvironment.Server.BuildWebDriver(new RemoteWebDriverOptions { Token = token }))
                {
                    await driver.NavigateToRoute("form-validation-page");

                    await driver.ExecuteTest("Bit.Tests.Implementations.Tests.ValidationTests.testValidationViewModelWithInValidBehavior");
                }
            }
        }
    }
}
