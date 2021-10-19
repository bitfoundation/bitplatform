using Bit.Test;
using Bit.Test.Server;
using Bit.Tests.Api.ApiControllers;
using FakeItEasy;
using Bit.Http.Contracts;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Remote;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Bit.Tests.HtmlClient.ViewModel.Implementation
{

    [TestClass]
    public class UIAutomationTests
    {
        [TestMethod]
        [TestCategory("HtmlClient"), TestCategory("Angular")]
        public virtual async Task TestGetBindingContextAndGetViewModel()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment(new TestEnvironmentArgs { UseRealServer = true }))
            {
                Token token = await testEnvironment.Server.LoginWithCredentials("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                using (WebDriver driver = testEnvironment.Server.BuildWebDriver(new WebDriverOptions { Token = token }))
                {
                    await driver.NavigateToRoute("repeat-page");

                    await driver.ExecuteTest("Bit.Tests.Implementations.Tests.UiAutomationTests.testGetBindingContextAndGetViewModel");
                }

                TestModelsController testModelsController = testEnvironment.GetObjects<TestModelsController>()
                    .Single();

                A.CallTo(() => testModelsController.GetTestModelsByStringPropertyValue(1))
                    .MustHaveHappenedOnceExactly();
            }
        }

        [TestMethod]
        [TestCategory("HtmlClient"), TestCategory("Angular")]
        public virtual async Task TestGettingSomeVariables()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment(new TestEnvironmentArgs { UseRealServer = true }))
            {
                Token token = await testEnvironment.Server.LoginWithCredentials("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                using (WebDriver driver = testEnvironment.Server.BuildWebDriver(new WebDriverOptions { Token = token }))
                {
                    await driver.ExecuteTest("Bit.Tests.Implementations.Tests.UiAutomationTests.testGettingSomeVariables", 5, 5, "Hi", new DateTimeOffset(2016, 1, 1, 1, 1, 1, TimeSpan.Zero), new { firstNum = 5, secondNum = 5, message = "Hi", date = new DateTimeOffset(2016, 1, 1, 1, 1, 1, TimeSpan.Zero) });
                }
            }
        }
    }
}