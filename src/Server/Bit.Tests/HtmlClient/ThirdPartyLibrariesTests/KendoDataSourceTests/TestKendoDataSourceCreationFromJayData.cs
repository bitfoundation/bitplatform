﻿using Bit.Core.Contracts;
using Bit.Test;
using Bit.Test.Server;
using Bit.Tests.Api.ApiControllers;
using FakeItEasy;
using Bit.Http.Contracts;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Remote;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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
                Token token = await testEnvironment.Server.LoginWithCredentials("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                using (WebDriver driver = testEnvironment.Server.BuildWebDriver(new WebDriverOptions { Token = token }))
                {
                    await driver.ExecuteTest("testKendoDataSourceCreationFromJayDataEntitySet");
                }

                TestModelsController testModelsController = testEnvironment.GetObjects<TestModelsController>()
                    .Single();

                A.CallTo(() => testModelsController.Get(A<CancellationToken>.Ignored))
                    .MustHaveHappenedOnceExactly();

                IEnumerable<ILogger> loggers = testEnvironment.GetObjects<ILogger>()
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
                Token token = await testEnvironment.Server.LoginWithCredentials("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                using (WebDriver driver = testEnvironment.Server.BuildWebDriver(new WebDriverOptions { Token = token }))
                {
                    await driver.ExecuteTest("testKendoDataSourceCreationFromJayDataODataFunctionCall");
                }

                TestModelsController testModelsController = testEnvironment.GetObjects<TestModelsController>()
                    .Single();

                A.CallTo(() => testModelsController.GetTestModelsByStringPropertyValue(1))
                    .MustHaveHappenedOnceExactly();

                IEnumerable<ILogger> loggers = testEnvironment.GetObjects<ILogger>()
                    .ToList();

                Assert.IsTrue((loggers.SelectMany(l => l.LogData).Any(ld => ld.Key == nameof(IRequestInformationProvider.DisplayUrl) && ((string)ld.Value).Contains("?$filter=(StringProperty eq 'String1')"))));
            }
        }
    }
}