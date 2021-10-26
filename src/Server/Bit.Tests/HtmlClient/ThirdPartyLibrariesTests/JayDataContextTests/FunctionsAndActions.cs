﻿using Bit.Test;
using Bit.Test.Server;
using Bit.Tests.Api.ApiControllers;
using FakeItEasy;
using Bit.Http.Contracts;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Remote;
using System.Linq;
using System.Threading.Tasks;

namespace Bit.Tests.HtmlClient.ThirdPartyLibrariesTests.JayDataContextTests
{
    [TestClass]
    public class FunctionsAndActions
    {
        [TestMethod]
        [TestCategory("HtmlClient"), TestCategory("JayDataContextOData")]
        public virtual async Task TestFunctionWithArgumentAndTakeMethodAfterThat()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment(new TestEnvironmentArgs { UseRealServer = true }))
            {
                Token token = await testEnvironment.Server.LoginWithCredentials("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                using (WebDriver driver = testEnvironment.Server.BuildWebDriver(new WebDriverOptions { Token = token }))
                {
                    await driver.ExecuteTest(@"testFunctionCallAndTakeMethodAfterThat");
                }

                TestModelsController testModelsController = testEnvironment.GetObjects<TestModelsController>()
                    .Single();

                A.CallTo(() => testModelsController.GetTestModelsByStringPropertyValue(1))
                    .MustHaveHappenedOnceExactly();
            }
        }

        [TestMethod]
        [TestCategory("HtmlClient"), TestCategory("JayDataContextOData")]
        public virtual async Task TestActionCall()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment(new TestEnvironmentArgs { UseRealServer = true }))
            {
                Token token = await testEnvironment.Server.LoginWithCredentials("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                using (WebDriver driver = testEnvironment.Server.BuildWebDriver(new WebDriverOptions { Token = token }))
                {
                    await driver.ExecuteTest(@"testActionCall");
                }

                TestModelsController testModelsController = testEnvironment.GetObjects<TestModelsController>()
                    .Single();

                A.CallTo(() => testModelsController.AreEqual(A<TestModelsController.FirstSecondParameters>.That.Matches(parameters => parameters.firstValue == 10 && parameters.secondValue == 10)))
                    .MustHaveHappenedOnceExactly();
            }
        }

        [TestMethod]
        [TestCategory("HtmlClient"), TestCategory("JayDataContextOData")]
        public virtual async Task PassNullTests()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment(new TestEnvironmentArgs { UseRealServer = true }))
            {
                Token token = await testEnvironment.Server.LoginWithCredentials("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                using (WebDriver driver = testEnvironment.Server.BuildWebDriver(new WebDriverOptions { Token = token }))
                {
                    await driver.ExecuteTest(@"passNullTests");
                }

                TestModelsController actionCallTest = testEnvironment.GetObjects<TestModelsController>()
                    .First();

                A.CallTo(() => actionCallTest.ActionForNullArg(A<TestModelsController.ActionForNullArgParameters>.Ignored))
                    .MustHaveHappenedOnceExactly();

                TestModelsController functionCallTest = testEnvironment.GetObjects<TestModelsController>()
                    .Last();

                A.CallTo(() => functionCallTest.FunctionForNullArg(null, "test"))
                    .MustHaveHappenedOnceExactly();
            }
        }

        [TestMethod]
        [TestCategory("HtmlClient"), TestCategory("JayDataContextOData")]
        public virtual async Task TestBatchCallODataFunctions()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment(new TestEnvironmentArgs { UseRealServer = true }))
            {
                Token token = await testEnvironment.Server.LoginWithCredentials("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                using (WebDriver driver = testEnvironment.Server.BuildWebDriver(new WebDriverOptions { Token = token }))
                {
                    await driver.ExecuteTest(@"testBatchCallODataFunctions");
                }

                TestModelsController testModelsController = testEnvironment.GetObjects<TestModelsController>()
                    .First();

                A.CallTo(() => testModelsController.GetTestModelsByStringPropertyValue(1))
                    .MustHaveHappenedOnceExactly();

                TestModelsController testModelsController2 = testEnvironment.GetObjects<TestModelsController>()
                    .Last();

                A.CallTo(() => testModelsController2.GetTestModelsByStringPropertyValue(2))
                    .MustHaveHappenedOnceExactly();
            }
        }

        [TestMethod]
        [TestCategory("HtmlClient"), TestCategory("JayDataContextOData")]
        public virtual async Task TestIEEE754Compatibility()
        {
            // see: https://github.com/OData/WebApi/issues/813

            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment(new TestEnvironmentArgs { UseRealServer = true }))
            {
                Token token = await testEnvironment.Server.LoginWithCredentials("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                using (WebDriver driver = testEnvironment.Server.BuildWebDriver(new WebDriverOptions { Token = token }))
                {
                    await driver.ExecuteTest(@"testIEEE754Compatibility");
                }

                A.CallTo(() => testEnvironment.GetObjects<TestModelsController>().ElementAt(0).TestIEEE754Compatibility(A<TestModelsController.TestIEEE754CompatibilityParameters>.That.Matches(p => p.val == decimal.MaxValue)))
                    .MustHaveHappenedOnceExactly();

                A.CallTo(() => testEnvironment.GetObjects<TestModelsController>().ElementAt(1).TestIEEE754Compatibility2(A<TestModelsController.TestIEEE754Compatibility2Parameters>.That.Matches(p => p.val == int.MaxValue)))
                    .MustHaveHappenedOnceExactly();

                A.CallTo(() => testEnvironment.GetObjects<TestModelsController>().ElementAt(2).TestIEEE754Compatibility3(A<TestModelsController.TestIEEE754Compatibility3Parameters>.That.Matches(p => p.val == long.MaxValue)))
                    .MustHaveHappenedOnceExactly();

                A.CallTo(() => testEnvironment.GetObjects<TestModelsController>().ElementAt(3).TestIEEE754Compatibility(A<TestModelsController.TestIEEE754CompatibilityParameters>.That.Matches(p => p.val == 12.2M)))
                    .MustHaveHappenedOnceExactly();

                A.CallTo(() => testEnvironment.GetObjects<TestModelsController>().ElementAt(4).TestIEEE754Compatibility(A<TestModelsController.TestIEEE754CompatibilityParameters>.That.Matches(p => p.val == 214748364711111.2M)))
                    .MustHaveHappenedOnceExactly();

                A.CallTo(() => testEnvironment.GetObjects<TestModelsController>().ElementAt(5).TestIEEE754Compatibility(A<TestModelsController.TestIEEE754CompatibilityParameters>.That.Matches(p => p.val == 214748364711111M)))
                    .MustHaveHappenedOnceExactly();

                A.CallTo(() => testEnvironment.GetObjects<TestModelsController>().ElementAt(6).TestDecimalSum(A<TestModelsController.FirstSecondValueDecimalParameters>.That.Matches(p => p.firstValue == 123456789123456789M && p.secondValue == 123456789123456789M)))
                    .MustHaveHappenedOnceExactly();
            }
        }

        [TestMethod]
        [TestCategory("HtmlClient"), TestCategory("JayDataContextOData")]
        public virtual async Task TestPassingArrayOfEntitiesToController()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment(new TestEnvironmentArgs { UseRealServer = true }))
            {
                Token token = await testEnvironment.Server.LoginWithCredentials("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                using (WebDriver driver = testEnvironment.Server.BuildWebDriver(new WebDriverOptions { Token = token }))
                {
                    await driver.ExecuteTest(@"testPassingArrayOfEntitiesToController");
                }

                A.CallTo(() => testEnvironment.GetObjects<ValidationSamplesController>().ElementAt(0)
                    .SubmitValidations(A<ValidationSamplesController.SubmitValidationsParameters>.That.Matches(parameters => parameters.validations.Count() == 2))).MustHaveHappenedOnceExactly();

                A.CallTo(() => testEnvironment.GetObjects<ValidationSamplesController>().ElementAt(1)
                    .SubmitValidations(A<ValidationSamplesController.SubmitValidationsParameters>.That.Matches(parameters => (parameters.arg == "A")))).MustHaveHappenedOnceExactly();
            }
        }
    }
}
