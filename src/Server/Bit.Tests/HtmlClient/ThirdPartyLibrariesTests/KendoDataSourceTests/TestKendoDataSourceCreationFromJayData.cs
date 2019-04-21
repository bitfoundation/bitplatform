using System.Linq;
using System.Threading;
using Bit.Core.Contracts;
using Bit.Test;
using Bit.Test.Implementations;
using Bit.Test.Server;
using Bit.Tests.Api.ApiControllers;
using FakeItEasy;
using IdentityModel.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Remote;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Bit.Tests.HtmlClient.ThirdPartyLibrariesTests.KendoDataSourceTests
{
    [TestClass]
    public class TestKendoDataSourceCreationFromJayData
    {
        [TestMethod]
        [TestCategory("HtmlClient"), TestCategory("KendoDataSource")]
        public virtual async Task TestKendoDataSourceCreationFromJayDataEntitySet()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment(new TestEnvironmentArgs { UseRealServer = true }))
            {
                TokenResponse token = await testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                using (RemoteWebDriver driver = testEnvironment.Server.BuildWebDriver(new RemoteWebDriverOptions { Token = token }))
                {
                    await driver.ExecuteTest("testKendoDataSourceCreationFromJayDataEntitySet");
                }

                TestModelsController testModelsController = TestDependencyManager.CurrentTestDependencyManager.Objects
                    .OfType<TestModelsController>()
                    .Single();

                A.CallTo(() => testModelsController.Get(A<CancellationToken>.Ignored))
                    .MustHaveHappenedOnceExactly();

                IEnumerable<ILogger> loggers = TestDependencyManager.CurrentTestDependencyManager.Objects
                    .OfType<ILogger>()
                    .ToList();

                Assert.IsTrue((loggers.SelectMany(l => l.LogData).Any(ld => ld.Key == nameof(IRequestInformationProvider.DisplayUrl) && ((string)ld.Value).Contains("?$filter=(StringProperty eq 'Test')"))));
            }
        }

        [TestMethod]
        [TestCategory("HtmlClient"), TestCategory("KendoDataSource")]
        public virtual async Task TestKendoDataSourceCreationFromJayDataODataFunctionCall()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment(new TestEnvironmentArgs { UseRealServer = true }))
            {
                TokenResponse token = await testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                using (RemoteWebDriver driver = testEnvironment.Server.BuildWebDriver(new RemoteWebDriverOptions { Token = token }))
                {
                    await driver.ExecuteTest("testKendoDataSourceCreationFromJayDataODataFunctionCall");
                }

                TestModelsController testModelsController = TestDependencyManager.CurrentTestDependencyManager.Objects
                    .OfType<TestModelsController>()
                    .Single();

                A.CallTo(() => testModelsController.GetTestModelsByStringPropertyValue(1))
                    .MustHaveHappenedOnceExactly();

                IEnumerable<ILogger> loggers = TestDependencyManager.CurrentTestDependencyManager.Objects
                    .OfType<ILogger>()
                    .ToList();

                Assert.IsTrue((loggers.SelectMany(l => l.LogData).Any(ld => ld.Key == nameof(IRequestInformationProvider.DisplayUrl) && ((string)ld.Value).Contains("?$filter=(StringProperty eq 'String1')"))));
            }
        }
    }
}