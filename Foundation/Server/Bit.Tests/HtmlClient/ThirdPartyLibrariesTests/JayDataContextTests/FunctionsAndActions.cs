using FakeItEasy;
using Foundation.Test.Api.ApiControllers;
using Foundation.Test.Core.Implementations;
using IdentityModel.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium.Remote;
using System.Linq;

namespace Foundation.Test.HtmlClient.ThirdPartyLibrariesTests.JayDataContextTests
{
    [TestClass]
    public class FunctionsAndActions
    {
        [TestMethod]
        [TestCategory("HtmlClient"), TestCategory("JayDataContextOData")]
        public virtual void TestFunctionWithArgumentAndTakeMethodAfterThat()
        {
            using (TestEnvironment testEnvironment = new TestEnvironment(new TestEnvironmentArgs { UseRealServer = true }))
            {
                TokenResponse token = testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientName: "TestResOwner");

                using (RemoteWebDriver driver = testEnvironment.Server.GetWebDriver(new RemoteWebDriverOptions { Token = token }))
                {
                    driver.ExecuteTest(@"testFunctionCallAndTakeMethodAfterThat");
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
        public virtual void TestActionCall()
        {
            using (TestEnvironment testEnvironment = new TestEnvironment(new TestEnvironmentArgs { UseRealServer = true }))
            {
                TokenResponse token = testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientName: "TestResOwner");

                using (RemoteWebDriver driver = testEnvironment.Server.GetWebDriver(new RemoteWebDriverOptions { Token = token }))
                {
                    driver.ExecuteTest(@"testActionCall");
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
        public virtual void PassNullTests()
        {
            using (TestEnvironment testEnvironment = new TestEnvironment(new TestEnvironmentArgs { UseRealServer = true }))
            {
                TokenResponse token = testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientName: "TestResOwner");

                using (RemoteWebDriver driver = testEnvironment.Server.GetWebDriver(new RemoteWebDriverOptions { Token = token }))
                {
                    driver.ExecuteTest(@"passNullTests");
                }

                TestModelsController actionCallTest = TestDependencyManager.CurrentTestDependencyManager.Objects
                    .OfType<TestModelsController>()
                    .First();

                A.CallTo(() => actionCallTest.ActionForNullArg(A<TestModelsController.ActionForNullArgParameters>.That.Matches(parameters => parameters.name == null)))
                    .MustHaveHappened(Repeated.Exactly.Once);

                TestModelsController functionCallTest = TestDependencyManager.CurrentTestDependencyManager.Objects
                    .OfType<TestModelsController>()
                    .Last();

                A.CallTo(() => functionCallTest.FunctionForNullArg(null))
                    .MustHaveHappened(Repeated.Exactly.Once);
            }
        }

        [TestMethod]
        [TestCategory("HtmlClient"), TestCategory("JayDataContextOData")]
        public virtual void TestBatchCallODataFunctions()
        {
            using (TestEnvironment testEnvironment = new TestEnvironment(new TestEnvironmentArgs { UseRealServer = true }))
            {
                TokenResponse token = testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientName: "TestResOwner");

                using (RemoteWebDriver driver = testEnvironment.Server.GetWebDriver(new RemoteWebDriverOptions { Token = token }))
                {
                    driver.ExecuteTest(@"testBatchCallODataFunctions");
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
        public virtual void TestIEEE754Compatibility()
        {
            // see: https://github.com/OData/WebApi/issues/813

            using (TestEnvironment testEnvironment = new TestEnvironment(new TestEnvironmentArgs { UseRealServer = true }))
            {
                TokenResponse token = testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientName: "TestResOwner");

                using (RemoteWebDriver driver = testEnvironment.Server.GetWebDriver(new RemoteWebDriverOptions { Token = token }))
                {
                    driver.ExecuteTest(@"testIEEE754Compatibility");
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
        public virtual void TestPassingArrayOfEntitiesToController()
        {
            using (TestEnvironment testEnvironment = new TestEnvironment(new TestEnvironmentArgs { UseRealServer = true }))
            {
                TokenResponse token = testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientName: "TestResOwner");

                using (RemoteWebDriver driver = testEnvironment.Server.GetWebDriver(new RemoteWebDriverOptions { Token = token }))
                {
                    driver.ExecuteTest(@"testPassingArrayOfEntitiesToController");
                }

                A.CallTo(() => TestDependencyManager.CurrentTestDependencyManager.Objects
                    .OfType<ValidationSamplesController>().ElementAt(0).SubmitValidations(A<ValidationSamplesController.SubmitValidationsParameters>.That.Matches(parameters => parameters.validations.Count() == 2))).MustHaveHappened(Repeated.Exactly.Once);

                A.CallTo(() => TestDependencyManager.CurrentTestDependencyManager.Objects
                    .OfType<ValidationSamplesController>().ElementAt(1).SubmitValidations(A<ValidationSamplesController.SubmitValidationsParameters>.That.Matches(parameters => (parameters.arg == "A")))).MustHaveHappened(Repeated.Exactly.Once);
            }
        }
    }
}
