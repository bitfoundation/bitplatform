using Bit.Test;
using Bit.Test.Implementations;
using Bit.Test.Server;
using Bit.Tests.Api.ApiControllers;
using Bit.Tests.Model.Dto;
using FakeItEasy;
using IdentityModel.Client;
using Microsoft.AspNet.OData;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Remote;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Bit.Tests.HtmlClient.ThirdPartyLibrariesTests.JayDataContextTests
{
    [TestClass]
    public class JayDataAndDtoSetControllerTests
    {
        [TestMethod]
        [TestCategory("HtmlClient"), TestCategory("JayDataDtoSetController")]
        public virtual async Task TestGetOfDtoSetController()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment(new TestEnvironmentArgs { UseRealServer = true }))
            {
                TokenResponse token = await testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                using (RemoteWebDriver driver = testEnvironment.Server.BuildWebDriver(new RemoteWebDriverOptions { Token = token }))
                {
                    await driver.ExecuteTest(@"testGetOfDtoSetController");
                }

                TestCustomersController testCustomersController = TestDependencyManager.CurrentTestDependencyManager.Objects
                    .OfType<TestCustomersController>()
                    .Single();

                A.CallTo(() => testCustomersController.GetAll(A<CancellationToken>.Ignored))
                    .MustHaveHappenedOnceExactly();
            }
        }

        [TestMethod]
        [TestCategory("HtmlClient"), TestCategory("JayDataDtoSetController")]
        public virtual async Task TestPatchOfDtoSetController()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment(new TestEnvironmentArgs { UseRealServer = true }))
            {
                TokenResponse token = await testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                using (RemoteWebDriver driver = testEnvironment.Server.BuildWebDriver(new RemoteWebDriverOptions { Token = token }))
                {
                    await driver.ExecuteTest(@"testPatchOfDtoSetController");
                }

                TestCustomersController testCustomersController = TestDependencyManager.CurrentTestDependencyManager.Objects
                    .OfType<TestCustomersController>()
                    .ElementAt(1);

                A.CallTo(() => testCustomersController.PartialUpdate(Guid.Parse("28e1ff65-da41-4fa3-8aeb-5196494b407d"), A<Delta<TestCustomerDto>>.That.Matches(d => d.GetInstance().Name == "TestCustomer?"), A<CancellationToken>.Ignored))
                    .MustHaveHappenedOnceExactly();
            }
        }
    }
}
