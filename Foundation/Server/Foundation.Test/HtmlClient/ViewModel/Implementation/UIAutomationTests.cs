using FakeItEasy;
using Foundation.Test.Api.ApiControllers;
using Foundation.Test.Core.Implementations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium.Remote;
using System;
using System.Linq;

namespace Foundation.Test.HtmlClient.ViewModel.Implementation
{

    [TestClass]
    public class UIAutomationTests
    {
        [TestMethod]
        [TestCategory("HtmlClient"), TestCategory("Angular")]
        public virtual void TestGetBindingContextAndGetFormViewModel()
        {
            using (TestEnvironment testEnvironment = new TestEnvironment(new TestEnvironmentArgs { UseRealServer = true }))
            {
                OAuthToken token = testEnvironment.Server.Login("ValidUserName", "ValidPassword");

                using (RemoteWebDriver driver = testEnvironment.Server.GetWebDriver(new RemoteWebDriverOptions { Token = token }))
                {
                    driver.NavigateToRoute("repeat-page");

                    driver.ExecuteTest("Foundation.Test.Implementations.Tests.UiAutomationTests.testGetBindingContextAndGetFormViewModel");
                }

                TestModelsController testModelsController = TestDependencyManager.CurrentTestDependencyManager.Objects
                    .OfType<TestModelsController>()
                    .Single();

                A.CallTo(() => testModelsController.GetTestModelsByStringPropertyValue(1))
                    .MustHaveHappened(Repeated.Exactly.Once);
            }
        }

        [TestMethod]
        [TestCategory("HtmlClient"), TestCategory("Angular")]
        public virtual void TestGettingSomeVariables()
        {
            using (TestEnvironment testEnvironment = new TestEnvironment(new TestEnvironmentArgs { UseRealServer = true }))
            {
                OAuthToken token = testEnvironment.Server.Login("ValidUserName", "ValidPassword");

                using (RemoteWebDriver driver = testEnvironment.Server.GetWebDriver(new RemoteWebDriverOptions { Token = token }))
                {
                    driver.ExecuteTest("Foundation.Test.Implementations.Tests.UiAutomationTests.testGettingSomeVariables", 5, 5, "Hi", new DateTimeOffset(2016, 1, 1, 1, 1, 1, TimeSpan.Zero), new { firstNum = 5, secondNum = 5, message = "Hi", date = new DateTimeOffset(2016, 1, 1, 1, 1, 1, TimeSpan.Zero) });
                }
            }
        }
    }
}