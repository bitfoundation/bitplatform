using FakeItEasy;
using Foundation.Test.Api.ApiControllers;
using Foundation.Test.Core.Implementations;
using Foundation.Test.Model.Dto;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium.Remote;
using System.Collections.Generic;
using System.Linq;
using System.Web.OData;

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
                OAuthToken token = testEnvironment.Server.Login("ValidUserName", "ValidPassword");

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
                OAuthToken token = testEnvironment.Server.Login("ValidUserName", "ValidPassword");

                using (RemoteWebDriver driver = testEnvironment.Server.GetWebDriver(new RemoteWebDriverOptions { Token = token }))
                {
                    driver.ExecuteTest(@"testActionCall");
                }

                TestModelsController testModelsController = TestDependencyManager.CurrentTestDependencyManager.Objects
                    .OfType<TestModelsController>()
                    .Single();

                A.CallTo(() => testModelsController.AreEqual(A<ODataActionParameters>.That.Matches(parameters => (int)parameters["firstValue"] == 10 && (int)parameters["secondValue"] == 10)))
                    .MustHaveHappened(Repeated.Exactly.Once);
            }
        }

        [TestMethod]
        [TestCategory("HtmlClient"), TestCategory("JayDataContextOData")]
        public virtual void TestBatchCallODataFunctions()
        {
            using (TestEnvironment testEnvironment = new TestEnvironment(new TestEnvironmentArgs { UseRealServer = true }))
            {
                OAuthToken token = testEnvironment.Server.Login("ValidUserName", "ValidPassword");

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
                OAuthToken token = testEnvironment.Server.Login("ValidUserName", "ValidPassword");

                using (RemoteWebDriver driver = testEnvironment.Server.GetWebDriver(new RemoteWebDriverOptions { Token = token }))
                {
                    driver.ExecuteTest(@"testIEEE754Compatibility");
                }

                A.CallTo(() => TestDependencyManager.CurrentTestDependencyManager.Objects
                    .OfType<TestModelsController>().ElementAt(0).TestIEEE754Compatibility(A<ODataActionParameters>.That.Matches(p => (decimal)p["val"] == decimal.MaxValue)))
                    .MustHaveHappened(Repeated.Exactly.Once);

                A.CallTo(() => TestDependencyManager.CurrentTestDependencyManager.Objects
                    .OfType<TestModelsController>().ElementAt(1).TestIEEE754Compatibility2(A<ODataActionParameters>.That.Matches(p => (int)p["val"] == int.MaxValue)))
                    .MustHaveHappened(Repeated.Exactly.Once);

                A.CallTo(() => TestDependencyManager.CurrentTestDependencyManager.Objects
                    .OfType<TestModelsController>().ElementAt(2).TestIEEE754Compatibility3(A<ODataActionParameters>.That.Matches(p => (long)p["val"] == long.MaxValue)))
                    .MustHaveHappened(Repeated.Exactly.Once);

                A.CallTo(() => TestDependencyManager.CurrentTestDependencyManager.Objects
                    .OfType<TestModelsController>().ElementAt(3).TestIEEE754Compatibility(A<ODataActionParameters>.That.Matches(p => (decimal)p["val"] == 12.2M)))
                    .MustHaveHappened(Repeated.Exactly.Once);

                A.CallTo(() => TestDependencyManager.CurrentTestDependencyManager.Objects
                    .OfType<TestModelsController>().ElementAt(4).TestIEEE754Compatibility(A<ODataActionParameters>.That.Matches(p => (decimal)p["val"] == 214748364711111.2M)))
                    .MustHaveHappened(Repeated.Exactly.Once);

                A.CallTo(() => TestDependencyManager.CurrentTestDependencyManager.Objects
                    .OfType<TestModelsController>().ElementAt(5).TestIEEE754Compatibility(A<ODataActionParameters>.That.Matches(p => (decimal)p["val"] == 214748364711111M)))
                    .MustHaveHappened(Repeated.Exactly.Once);

                A.CallTo(() => TestDependencyManager.CurrentTestDependencyManager.Objects
                    .OfType<TestModelsController>().ElementAt(6).TestDecimalSum(A<ODataActionParameters>.That.Matches(p => (decimal)p["firstValue"] == 123456789123456789m && (decimal)p["secondValue"] == 123456789123456789m)))
                    .MustHaveHappened(Repeated.Exactly.Once);
            }
        }

        [TestMethod]
        [TestCategory("HtmlClient"), TestCategory("JayDataContextOData")]
        public virtual void TestPassingArrayOfEntitiesToController()
        {
            using (TestEnvironment testEnvironment = new TestEnvironment(new TestEnvironmentArgs { UseRealServer = true }))
            {
                OAuthToken token = testEnvironment.Server.Login("ValidUserName", "ValidPassword");

                using (RemoteWebDriver driver = testEnvironment.Server.GetWebDriver(new RemoteWebDriverOptions { Token = token }))
                {
                    driver.ExecuteTest(@"testPassingArrayOfEntitiesToController");
                }

                A.CallTo(() => TestDependencyManager.CurrentTestDependencyManager.Objects
                    .OfType<ValidationSamplesController>().ElementAt(0).SubmitValidations(A<ODataActionParameters>.That.Matches(parameters => ((IEnumerable<ValidationSampleDto>)parameters["validations"]).Count() == 2))).MustHaveHappened(Repeated.Exactly.Once);

                A.CallTo(() => TestDependencyManager.CurrentTestDependencyManager.Objects
                    .OfType<ValidationSamplesController>().ElementAt(1).SubmitValidations(A<ODataActionParameters>.That.Matches(parameters => ((string)parameters["arg"] == "A")))).MustHaveHappened(Repeated.Exactly.Once);
            }
        }
    }
}
