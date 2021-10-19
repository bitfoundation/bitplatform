﻿using Bit.Test;
using Bit.Test.Server;
using Bit.Tests.Api.ApiControllers;
using Bit.Tests.Model.DomainModels;
using FakeItEasy;
using Bit.Http.Contracts;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Remote;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Bit.Tests.HtmlClient.ViewModel.Implementation
{
    [TestClass]
    public class RadTests
    {
        [TestMethod]
        [TestCategory("HtmlClient"), TestCategory("Rad")]
        public virtual async Task RadComboViewModelTest()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment(new TestEnvironmentArgs { UseRealServer = true }))
            {
                Token token = await testEnvironment.Server.LoginWithCredentials("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                using (WebDriver driver = testEnvironment.Server.BuildWebDriver(new WebDriverOptions { Token = token }))
                {
                    await driver.NavigateToRoute("rad-combo-page");

                    await driver.ExecuteTest("Bit.Tests.Implementations.Tests.RadTests.testRadComboViewModel");
                }

                TestModelsController testModelsController = testEnvironment.GetObjects<TestModelsController>()
                    .Single();

                A.CallTo(() => testModelsController.GetTestModelsByStringPropertyValue(1))
                    .MustHaveHappenedOnceExactly();

                ParentEntitiesController parentEntitiesController = testEnvironment.GetObjects<ParentEntitiesController>()
                    .Single();

                A.CallTo(() => parentEntitiesController.GetTestData())
                    .MustHaveHappenedOnceExactly();
            }
        }

        [TestMethod]
        [TestCategory("HtmlClient"), TestCategory("Rad")]
        public virtual async Task RadGridViewModelAddTest()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment(new TestEnvironmentArgs { UseRealServer = true }))
            {
                Token token = await testEnvironment.Server.LoginWithCredentials("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                using (WebDriver driver = testEnvironment.Server.BuildWebDriver(new WebDriverOptions { Token = token }))
                {
                    await driver.NavigateToRoute("rad-grid-page");

                    await driver.ExecuteTest("Bit.Tests.Implementations.Tests.RadTests.testRadGridViewModelAdd");
                }

                ParentEntitiesController parentEntitiesController = testEnvironment.GetObjects<ParentEntitiesController>()
                    .Last();

                A.CallTo(() => parentEntitiesController.Create(A<ParentEntity>.That.Matches(p => p.Name == "!"), A<CancellationToken>.Ignored))
                    .MustHaveHappenedOnceExactly();
            }
        }
    }
}
