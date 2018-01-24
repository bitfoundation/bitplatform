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
                TokenResponse token = await testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                using (RemoteWebDriver driver = testEnvironment.Server.BuildWebDriver(new RemoteWebDriverOptions { Token = token }))
                {
                    await driver.ExecuteTest(@"testFunctionCallAndTakeMethodAfterThat");
                }

                TestModelsController testModelsController = TestDependencyManager.CurrentTestDependencyManager.Objects
                    .OfType<TestModelsController>()
                    .Single();

                A.CallTo(() => testModelsController.GetTestModelsByStringPropertyValue(1))
                    .MustHaveHappened(Repeated.Exactly.Once);
            }
        }

        [TestMethod]
        [TestCategory("HtmlClient"), TestCategory("JayDataContextOData")]
        public virtual async Task TestActionCall()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment(new TestEnvironmentArgs { UseRealServer = true }))
            {
                TokenResponse token = await testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                using (RemoteWebDriver driver = testEnvironment.Server.BuildWebDriver(new RemoteWebDriverOptions { Token = token }))
                {
                    await driver.ExecuteTest(@"testActionCall");
                }

                TestModelsController testModelsController = TestDependencyManager.CurrentTestDependencyManager.Objects
                    .OfType<TestModelsController>()
                    .Single();

                A.CallTo(() => testModelsController.AreEqual(A<TestModelsController.FirstSecondParameters>.That.Matches(parameters => parameters.firstValue == 10 && parameters.secondValue == 10)))
                    .MustHaveHappened(Repeated.Exactly.Once);
            }
        }

        [TestMethod]
        [TestCategory("HtmlClient"), TestCategory("JayDataContextOData")]
        public virtual async Task PassNullTests()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment(new TestEnvironmentArgs { UseRealServer = true }))
            {
                TokenResponse token = await testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                using (RemoteWebDriver driver = testEnvironment.Server.BuildWebDriver(new RemoteWebDriverOptions { Token = token }))
                {
                    await driver.ExecuteTest(@"passNullTests");
                }

                TestModelsController actionCallTest = TestDependencyManager.CurrentTestDependencyManager.Objects
                    .OfType<TestModelsController>()
                    .First();

                A.CallTo(() => actionCallTest.ActionForNullArg(A<TestModelsController.ActionForNullArgParameters>.Ignored))
                    .MustHaveHappened(Repeated.Exactly.Once);

                TestModelsController functionCallTest = TestDependencyManager.CurrentTestDependencyManager.Objects
                    .OfType<TestModelsController>()
                    .Last();

                A.CallTo(() => functionCallTest.FunctionForNullArg(null, "test"))
                    .MustHaveHappened(Repeated.Exactly.Once);
            }
        }

        [TestMethod]
        [TestCategory("HtmlClient"), TestCategory("JayDataContextOData")]
        public virtual async Task TestBatchCallODataFunctions()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment(new TestEnvironmentArgs { UseRealServer = true }))
            {
                TokenResponse token = await testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                using (RemoteWebDriver driver = testEnvironment.Server.BuildWebDriver(new RemoteWebDriverOptions { Token = token }))
                {
                    await driver.ExecuteTest(@"testBatchCallODataFunctions");
                }

                TestModelsController testModelsController = TestDependencyManager.CurrentTestDependencyManager.Objects
                    .OfType<TestModelsController>()
                    .First();

                A.CallTo(() => testModelsController.GetTestModelsByStringPropertyValue(1))
                    .MustHaveHappened(Repeated.Exactly.Once);

                TestModelsController testModelsController2 = TestDependencyManager.CurrentTestDependencyManager.Objects
                    .OfType<TestModelsController>()
                    .Last();

                A.CallTo(() => testModelsController2.GetTestModelsByStringPropertyValue(2))
                    .MustHaveHappened(Repeated.Exactly.Once);
            }
        }

        [TestMethod]
        [TestCategory("HtmlClient"), TestCategory("JayDataContextOData")]
        public virtual async Task TestIEEE754Compatibility()
        {
            // see: https://github.com/OData/WebApi/issues/813

            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment(new TestEnvironmentArgs { UseRealServer = true }))
            {
                TokenResponse token = await testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                using (RemoteWebDriver driver = testEnvironment.Server.BuildWebDriver(new RemoteWebDriverOptions { Token = token }))
                {
                    await driver.ExecuteTest(@"testIEEE754Compatibility");
                }

                A.CallTo(() => TestDependencyManager.CurrentTestDependencyManager.Objects
                    .OfType<TestModelsController>().ElementAt(0).TestIEEE754Compatibility(A<TestModelsController.TestIEEE754CompatibilityParameters>.That.Matches(p => p.val == decimal.MaxValue)))
                    .MustHaveHappened(Repeated.Exactly.Once);

                A.CallTo(() => TestDependencyManager.CurrentTestDependencyManager.Objects
                    .OfType<TestModelsController>().ElementAt(1).TestIEEE754Compatibility2(A<TestModelsController.TestIEEE754Compatibility2Parameters>.That.Matches(p => p.val == int.MaxValue)))
                    .MustHaveHappened(Repeated.Exactly.Once);

                A.CallTo(() => TestDependencyManager.CurrentTestDependencyManager.Objects
                    .OfType<TestModelsController>().ElementAt(2).TestIEEE754Compatibility3(A<TestModelsController.TestIEEE754Compatibility3Parameters>.That.Matches(p => p.val == long.MaxValue)))
                    .MustHaveHappened(Repeated.Exactly.Once);

                A.CallTo(() => TestDependencyManager.CurrentTestDependencyManager.Objects
                    .OfType<TestModelsController>().ElementAt(3).TestIEEE754Compatibility(A<TestModelsController.TestIEEE754CompatibilityParameters>.That.Matches(p => p.val == 12.2M)))
                    .MustHaveHappened(Repeated.Exactly.Once);

                A.CallTo(() => TestDependencyManager.CurrentTestDependencyManager.Objects
                    .OfType<TestModelsController>().ElementAt(4).TestIEEE754Compatibility(A<TestModelsController.TestIEEE754CompatibilityParameters>.That.Matches(p => p.val == 214748364711111.2M)))
                    .MustHaveHappened(Repeated.Exactly.Once);

                A.CallTo(() => TestDependencyManager.CurrentTestDependencyManager.Objects
                    .OfType<TestModelsController>().ElementAt(5).TestIEEE754Compatibility(A<TestModelsController.TestIEEE754CompatibilityParameters>.That.Matches(p => p.val == 214748364711111M)))
                    .MustHaveHappened(Repeated.Exactly.Once);

                A.CallTo(() => TestDependencyManager.CurrentTestDependencyManager.Objects
                    .OfType<TestModelsController>().ElementAt(6).TestDecimalSum(A<TestModelsController.FirstSecondValueDecimalParameters>.That.Matches(p => p.firstValue == 123456789123456789M && p.secondValue == 123456789123456789M)))
                    .MustHaveHappened(Repeated.Exactly.Once);
            }
        }

        [TestMethod]
        [TestCategory("HtmlClient"), TestCategory("JayDataContextOData")]
        public virtual async Task TestPassingArrayOfEntitiesToController()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment(new TestEnvironmentArgs { UseRealServer = true }))
            {
                TokenResponse token = await testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                using (RemoteWebDriver driver = testEnvironment.Server.BuildWebDriver(new RemoteWebDriverOptions { Token = token }))
                {
                    await driver.ExecuteTest(@"testPassingArrayOfEntitiesToController");
                }

                A.CallTo(() => TestDependencyManager.CurrentTestDependencyManager.Objects
                    .OfType<ValidationSamplesController>().ElementAt(0).SubmitValidations(A<ValidationSamplesController.SubmitValidationsParameters>.That.Matches(parameters => parameters.validations.Count() == 2))).MustHaveHappened(Repeated.Exactly.Once);

                A.CallTo(() => TestDependencyManager.CurrentTestDependencyManager.Objects
                    .OfType<ValidationSamplesController>().ElementAt(1).SubmitValidations(A<ValidationSamplesController.SubmitValidationsParameters>.That.Matches(parameters => (parameters.arg == "A")))).MustHaveHappened(Repeated.Exactly.Once);
            }
        }
    }
}
