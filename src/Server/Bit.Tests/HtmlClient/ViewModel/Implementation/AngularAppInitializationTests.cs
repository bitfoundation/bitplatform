using System.Linq;
using Bit.Test;
using Bit.Test.Core.Implementations;
using Bit.Test.Server;
using Bit.Tests.Api.ApiControllers;
using FakeItEasy;
using IdentityModel.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Remote;
using System.Threading.Tasks;

namespace Bit.Tests.HtmlClient.ViewModel.Implementation
{
    [TestClass]
    public class AngularAppInitializationTests
    {
        [TestMethod]
        [TestCategory("HtmlClient"), TestCategory("Angular")]
        public virtual async Task SimpleFormViewModelTest()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment(new TestEnvironmentArgs { UseRealServer = true }))
            {
                TokenResponse token = await testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                using (RemoteWebDriver driver = testEnvironment.Server.GetWebDriver(new RemoteWebDriverOptions { Token = token }))
                {
                    driver.NavigateToRoute("simple-page");

                    Assert.AreEqual("12=12", driver.GetElementById("test").GetAttribute("innerText"));
                }
            }
        }

        [TestMethod]
        [TestCategory("HtmlClient"), TestCategory("Angular")]
        public virtual async Task RouteParameterFormViewModelTest()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment(new TestEnvironmentArgs { UseRealServer = true }))
            {
                TokenResponse token = await testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                using (RemoteWebDriver driver = testEnvironment.Server.GetWebDriver(new RemoteWebDriverOptions { Token = token }))
                {
                    driver.NavigateToRoute("route-parameter-page/value");

                    driver.WaitForCondition(d => d.Title == "value");
                }
            }
        }

        [TestMethod]
        [TestCategory("HtmlClient"), TestCategory("Angular")]
        public virtual async Task TestNestedRoute()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment(new TestEnvironmentArgs { UseRealServer = true }))
            {
                TokenResponse token = await testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                using (RemoteWebDriver driver = testEnvironment.Server.GetWebDriver(new RemoteWebDriverOptions { Token = token }))
                {
                    driver.NavigateToRoute("nested-route-page");

                    driver.GetElementById("gotonextpart").Click();

                    Assert.AreEqual("1", driver.GetElementById("parameter").GetAttribute("innerText"));
                }
            }
        }

        [TestMethod]
        [TestCategory("HtmlClient"), TestCategory("Angular")]
        public virtual async Task AngularServiceUsageFormViewModelTest()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment(new TestEnvironmentArgs { UseRealServer = true }))
            {
                TokenResponse token = await testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                using (RemoteWebDriver driver = testEnvironment.Server.GetWebDriver(new RemoteWebDriverOptions { Token = token }))
                {
                    driver.NavigateToRoute("angular-service-usage-page");

                    driver.WaitForCondition(d => d.Title == "done");
                }
            }
        }

        [TestMethod]
        [TestCategory("HtmlClient"), TestCategory("Angular")]
        public virtual async Task EntityContextUsageFormViewModelTest()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment(new TestEnvironmentArgs { UseRealServer = true }))
            {
                TokenResponse token = await testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                using (RemoteWebDriver driver = testEnvironment.Server.GetWebDriver(new RemoteWebDriverOptions { Token = token }))
                {
                    driver.NavigateToRoute("entity-context-usage-page");

                    IWebElement label = driver.GetElementById("test");

                    driver.WaitForCondition(d => label.Text == "32=32");
                }
            }
        }

        [TestMethod]
        [TestCategory("HtmlClient"), TestCategory("Angular")]
        public virtual async Task AsyncFormViewModelTest()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment(new TestEnvironmentArgs { UseRealServer = true }))
            {
                TokenResponse token = await testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                using (RemoteWebDriver driver = testEnvironment.Server.GetWebDriver(new RemoteWebDriverOptions { Token = token }))
                {
                    driver.NavigateToRoute("async-page");

                    driver.ExecuteTest("Bit.Tests.Implementations.Tests.AngularAppInitializationTests.testAsyncFormViewModel");
                }

                TestModelsController testModelsController = TestDependencyManager.CurrentTestDependencyManager.Objects
                    .OfType<TestModelsController>()
                    .Single();

                A.CallTo(() => testModelsController.Sum(A<TestModelsController.FirstSecondParameters>.That.Matches(parameters => parameters.firstValue == 10 && parameters.secondValue == 20)))
                    .MustHaveHappened(Repeated.Exactly.Once);
            }
        }

        [TestMethod]
        [TestCategory("HtmlClient"), TestCategory("Angular")]
        public virtual async Task SetAsDefaultFormViewModelTest()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment(new TestEnvironmentArgs { UseRealServer = true }))
            {
                TokenResponse token = await testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                using (RemoteWebDriver driver = testEnvironment.Server.GetWebDriver(new RemoteWebDriverOptions { Token = token }))
                {
                    driver.WaitForCondition(d => d.Title == "Nested View");

                    Assert.AreEqual("Nested View", driver.Title);

                    Assert.IsTrue(driver.Url.ToLowerInvariant().EndsWith("nested-route-page/first-part-page"));
                }
            }
        }
    }
}
