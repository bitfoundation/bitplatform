using FakeItEasy;
using Foundation.Test.Api.ApiControllers;
using Foundation.Test.Core.Implementations;
using Foundation.Test.Model.Dto;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium.Remote;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.OData;

namespace Foundation.Test.HtmlClient.ThirdPartyLibrariesTests.JayDataContextTests
{
    [TestClass]
    public class JayDataAndDtoSetControllerTests
    {
        [TestMethod]
        [TestCategory("HtmlClient"), TestCategory("JayDataDtoSetController")]
        public virtual void TestGetOfDtoSetController()
        {
            using (TestEnvironment testEnvironment = new TestEnvironment(new TestEnvironmentArgs { UseRealServer = true }))
            {
                OAuthToken token = testEnvironment.Server.Login("ValidUserName", "ValidPassword");

                using (RemoteWebDriver driver = testEnvironment.Server.GetWebDriver(new RemoteWebDriverOptions { Token = token }))
                {
                    driver.ExecuteTest(@"testGetOfDtoSetController");
                }

                TestCustomersController testCustomersController = TestDependencyManager.CurrentTestDependencyManager.Objects
                    .OfType<TestCustomersController>()
                    .Single();

                A.CallTo(() => testCustomersController.GetAll())
                    .MustHaveHappened(Repeated.Exactly.Once);
            }
        }

        [TestMethod]
        [TestCategory("HtmlClient"), TestCategory("JayDataDtoSetController")]
        public virtual void TestPatchOfDtoSetController()
        {
            using (TestEnvironment testEnvironment = new TestEnvironment(new TestEnvironmentArgs { UseRealServer = true }))
            {
                OAuthToken token = testEnvironment.Server.Login("ValidUserName", "ValidPassword");

                using (RemoteWebDriver driver = testEnvironment.Server.GetWebDriver(new RemoteWebDriverOptions { Token = token }))
                {
                    driver.ExecuteTest(@"testPatchOfDtoSetController");
                }

                TestCustomersController testCustomersController = TestDependencyManager.CurrentTestDependencyManager.Objects
                    .OfType<TestCustomersController>()
                    .ElementAt(1);

                A.CallTo(() => testCustomersController.Update(Guid.Parse("28e1ff65-da41-4fa3-8aeb-5196494b407d"), A<Delta<TestCustomerDto>>.That.Matches(d => d.GetInstance().Name == "TestCustomer?"), A<CancellationToken>.Ignored))
                    .MustHaveHappened(Repeated.Exactly.Once);
            }
        }
    }
}
