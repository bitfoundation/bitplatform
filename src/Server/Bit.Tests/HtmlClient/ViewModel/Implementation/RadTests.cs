using System.Linq;
using System.Threading;
using Bit.Test;
using Bit.Test.Core.Implementations;
using Bit.Test.Server;
using Bit.Tests.Api.ApiControllers;
using Bit.Tests.Model.DomainModels;
using FakeItEasy;
using IdentityModel.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Remote;
using System.Threading.Tasks;
using OpenQA.Selenium.Support.UI;
using System;

namespace Bit.Tests.HtmlClient.ViewModel.Implementation
{
    [TestClass]
    public class RadTests
    {
        [TestMethod]
        [TestCategory("HtmlClient"), TestCategory("Rad")]
        public virtual async Task RadComboFormViewModelTest()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment(new TestEnvironmentArgs { UseRealServer = true }))
            {
                TokenResponse token = await testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                using (RemoteWebDriver driver = testEnvironment.Server.GetWebDriver(new RemoteWebDriverOptions { Token = token }))
                {
                    await driver.NavigateToRoute("rad-combo-page");

                    await driver.ExecuteTest("Bit.Tests.Implementations.Tests.RadTests.testRadComboFormViewModel");
                }

                TestModelsController testModelsController = TestDependencyManager.CurrentTestDependencyManager.Objects
                    .OfType<TestModelsController>()
                    .Single();

                A.CallTo(() => testModelsController.GetTestModelsByStringPropertyValue(1))
                    .MustHaveHappened(Repeated.Exactly.Once);

                ParentEntitiesController parentEntitiesController = TestDependencyManager.CurrentTestDependencyManager.Objects
                    .OfType<ParentEntitiesController>()
                    .Single();

                A.CallTo(() => parentEntitiesController.GetTestData())
                    .MustHaveHappened(Repeated.Exactly.Once);
            }
        }

        [TestMethod]
        [TestCategory("HtmlClient"), TestCategory("Rad")]
        public virtual async Task RadGridFormViewModelAddTest()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment(new TestEnvironmentArgs { UseRealServer = true }))
            {
                TokenResponse token = await testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                using (RemoteWebDriver driver = testEnvironment.Server.GetWebDriver(new RemoteWebDriverOptions { Token = token }))
                {
                    await driver.NavigateToRoute("rad-grid-page");

                    await driver.ExecuteTest("Bit.Tests.Implementations.Tests.RadTests.testRadGridFormViewModelAdd");
                }

                ParentEntitiesController parentEntitiesController = TestDependencyManager.CurrentTestDependencyManager.Objects
                    .OfType<ParentEntitiesController>()
                    .Last();

                A.CallTo(() => parentEntitiesController.Create(A<ParentEntity>.That.Matches(p => p.Name == "!"), A<CancellationToken>.Ignored))
                    .MustHaveHappened(Repeated.Exactly.Once);
            }
        }
    }
}
