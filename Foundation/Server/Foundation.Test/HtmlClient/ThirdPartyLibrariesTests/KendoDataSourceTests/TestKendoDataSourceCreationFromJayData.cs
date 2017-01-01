using FakeItEasy;
using Foundation.Core.Contracts;
using Foundation.Test.Api.ApiControllers;
using Foundation.Test.Core.Implementations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium.Remote;
using System.Linq;

namespace Foundation.Test.HtmlClient.ThirdPartyLibrariesTests.KendoDataSourceTests
{
    [TestClass]
    public class TestKendoDataSourceCreationFromJayData
    {
        [TestMethod]
        [TestCategory("HtmlClient"), TestCategory("KendoDataSource")]
        public virtual void TestKendoDataSourceCreationFromJayDataEntitySet()
        {
            using (TestEnvironment testEnvironment = new TestEnvironment(new TestEnvironmentArgs { UseRealServer = true }))
            {
                OAuthToken token = testEnvironment.Server.Login("ValidUserName", "ValidPassword");

                using (RemoteWebDriver driver = testEnvironment.Server.GetWebDriver(new RemoteWebDriverOptions { Token = token }))
                {
                    driver.ExecuteTest("testKendoDataSourceCreationFromJayDataEntitySet");
                }

                TestModelsController testModelsController = TestDependencyManager.CurrentTestDependencyManager.Objects
                    .OfType<TestModelsController>()
                    .Single();

                A.CallTo(() => testModelsController.Get())
                    .MustHaveHappened(Repeated.Exactly.Once);

                ILogger logger = TestDependencyManager.CurrentTestDependencyManager.Objects
                    .OfType<ILogger>()
                    .Last();

                Assert.IsTrue(((string)logger.LogData.Single(ld => ld.Key == nameof(IRequestInformationProvider.RequestUri)).Value).Contains("?$filter=(StringProperty eq 'Test')"));
            }
        }

        [TestMethod]
        [TestCategory("HtmlClient"), TestCategory("KendoDataSource")]
        public virtual void TestKendoDataSourceCreationFromJayDataODataFunctionCall()
        {
            using (TestEnvironment testEnvironment = new TestEnvironment(new TestEnvironmentArgs { UseRealServer = true }))
            {
                OAuthToken token = testEnvironment.Server.Login("ValidUserName", "ValidPassword");

                using (RemoteWebDriver driver = testEnvironment.Server.GetWebDriver(new RemoteWebDriverOptions { Token = token }))
                {
                    driver.ExecuteTest("testKendoDataSourceCreationFromJayDataODataFunctionCall");
                }

                TestModelsController testModelsController = TestDependencyManager.CurrentTestDependencyManager.Objects
                    .OfType<TestModelsController>()
                    .Single();

                A.CallTo(() => testModelsController.GetTestModelsByStringPropertyValue(1))
                    .MustHaveHappened(Repeated.Exactly.Once);

                ILogger logger = TestDependencyManager.CurrentTestDependencyManager.Objects
                    .OfType<ILogger>()
                    .Last();

                Assert.IsTrue(((string)logger.LogData.Single(ld => ld.Key == nameof(IRequestInformationProvider.RequestUri)).Value).Contains("?$filter=(StringProperty eq 'String1')"));
            }
        }
    }
}