using FakeItEasy;
using Foundation.Test.Api.ApiControllers;
using Foundation.Test.Core.Implementations;
using Foundation.Test.Model.DomainModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium.Remote;
using System.Linq;
using System.Threading;

namespace Foundation.Test.HtmlClient.ViewModel.Implementation
{
    [TestClass]
    public class RadTests
    {
        [TestMethod]
        [TestCategory("HtmlClient"), TestCategory("Rad")]
        public virtual void RadComboFormViewModelTest()
        {
            using (TestEnvironment testEnvironment = new TestEnvironment(new TestEnvironmentArgs { UseRealServer = true }))
            {
                OAuthToken token = testEnvironment.Server.Login("ValidUserName", "ValidPassword");

                using (RemoteWebDriver driver = testEnvironment.Server.GetWebDriver(new RemoteWebDriverOptions { Token = token }))
                {
                    driver.NavigateToRoute("rad-combo-page");

                    driver.ExecuteTest("Foundation.Test.Implementations.Tests.RadTests.testRadComboFormViewModel");
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

        [Ignore]
        [TestMethod]
        [TestCategory("HtmlClient"), TestCategory("Rad")]
        public virtual void RadGridFormViewModelAddTest()
        {
            using (TestEnvironment testEnvironment = new TestEnvironment(new TestEnvironmentArgs { UseRealServer = true }))
            {
                OAuthToken token = testEnvironment.Server.Login("ValidUserName", "ValidPassword");

                using (RemoteWebDriver driver = testEnvironment.Server.GetWebDriver(new RemoteWebDriverOptions { Token = token }))
                {
                    driver.NavigateToRoute("rad-grid-page");

                    driver.ExecuteTest("Foundation.Test.Implementations.Tests.RadTests.testRadGridFormViewModelAdd");
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
