using FakeItEasy;
using Foundation.Test.Api.ApiControllers;
using Foundation.Test.Core.Implementations;
using Foundation.Test.Model.DomainModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium.Remote;
using System.Linq;
using System.Threading;

namespace Foundation.Test.HtmlClient.ThirdPartyLibrariesTests.JayDataContextTests
{
    [TestClass]
    public class CrudTests
    {
        [TestMethod]
        [TestCategory("HtmlClient"), TestCategory("JayDataContextOData")]
        public virtual void TestInsert()
        {
            using (TestEnvironment testEnvironment = new TestEnvironment(new TestEnvironmentArgs { UseRealServer = true }))
            {
                OAuthToken token = testEnvironment.Server.Login("ValidUserName", "ValidPassword");

                using (RemoteWebDriver driver = testEnvironment.Server.GetWebDriver(new RemoteWebDriverOptions { Token = token }))
                {
                    driver.ExecuteTest("testInsert");
                }

                TestModelsController testModelsController = TestDependencyManager.CurrentTestDependencyManager.Objects
                    .OfType<TestModelsController>()
                    .Single();

                A.CallTo(() => testModelsController.Create(A<TestModel>.That.Matches(m => m.StringProperty == "Test"), A<CancellationToken>.Ignored))
                    .MustHaveHappened(Repeated.Exactly.Once);
            }
        }

        [TestMethod]
        [TestCategory("HtmlClient"), TestCategory("JayDataContextOData")]
        public virtual void TestGetAllAndFilter()
        {
            using (TestEnvironment testEnvironment = new TestEnvironment(new TestEnvironmentArgs { UseRealServer = true }))
            {
                OAuthToken token = testEnvironment.Server.Login("ValidUserName", "ValidPassword");

                using (RemoteWebDriver driver = testEnvironment.Server.GetWebDriver(new RemoteWebDriverOptions { Token = token }))
                {
                    driver.ExecuteTest(@"testGetAllAndFilter");
                }

                ParentEntitiesController parentEntitiesController = TestDependencyManager.CurrentTestDependencyManager.Objects
                    .OfType<ParentEntitiesController>()
                    .Single();

                A.CallTo(() => parentEntitiesController.Get())
                    .MustHaveHappened(Repeated.Exactly.Once);
            }
        }

        [TestMethod]
        [TestCategory("HtmlClient"), TestCategory("JayDataContextOData")]
        public virtual void TestBatchReadRequest()
        {
            using (TestEnvironment testEnvironment = new TestEnvironment(new TestEnvironmentArgs { UseRealServer = true }))
            {
                OAuthToken token = testEnvironment.Server.Login("ValidUserName", "ValidPassword");

                using (RemoteWebDriver driver = testEnvironment.Server.GetWebDriver(new RemoteWebDriverOptions { Token = token }))
                {
                    driver.ExecuteTest(@"testBatchReadRequest");
                }

                ParentEntitiesController parentEntitiesController = TestDependencyManager.CurrentTestDependencyManager.Objects
                    .OfType<ParentEntitiesController>()
                    .Single();

                A.CallTo(() => parentEntitiesController.Get())
                    .MustHaveHappened(Repeated.Exactly.Once);

                TestModelsController testModelsController = TestDependencyManager.CurrentTestDependencyManager.Objects
                    .OfType<TestModelsController>()
                    .Single();

                A.CallTo(() => testModelsController.Get())
                    .MustHaveHappened(Repeated.Exactly.Once);
            }
        }
    }
}
