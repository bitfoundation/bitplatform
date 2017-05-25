using System.Linq;
using System.Threading;
using Bit.Core.Contracts;
using Bit.Test;
using Bit.Test.Core.Implementations;
using Bit.Test.Server;
using Bit.Tests.Api.ApiControllers;
using FakeItEasy;
using IdentityModel.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Remote;

namespace Bit.Tests.HtmlClient.ThirdPartyLibrariesTests.KendoDataSourceTests
{
    [TestClass]
    public class TestKendoDataSourceCreationFromJayData
    {
        [TestMethod]
        [TestCategory("HtmlClient"), TestCategory("KendoDataSource")]
        public virtual void TestKendoDataSourceCreationFromJayDataEntitySet()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment(new TestEnvironmentArgs { UseRealServer = true }))
            {
                TokenResponse token = testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientName: "TestResOwner");

                using (RemoteWebDriver driver = testEnvironment.Server.GetWebDriver(new RemoteWebDriverOptions { Token = token }))
                {
                    driver.ExecuteTest("testKendoDataSourceCreationFromJayDataEntitySet");
                }

                TestModelsController testModelsController = TestDependencyManager.CurrentTestDependencyManager.Objects
                    .OfType<TestModelsController>()
                    .Single();

                A.CallTo(() => testModelsController.Get(A<CancellationToken>.Ignored))
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
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment(new TestEnvironmentArgs { UseRealServer = true }))
            {
                TokenResponse token = testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientName: "TestResOwner");

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