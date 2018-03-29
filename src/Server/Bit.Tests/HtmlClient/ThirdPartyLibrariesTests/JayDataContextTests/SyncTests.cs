using Bit.Test;
using Bit.Test.Core.Implementations;
using Bit.Test.Server;
using Bit.Tests.Api.ApiControllers;
using Bit.Tests.Model.Dto;
using FakeItEasy;
using IdentityModel.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Remote;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web.OData;

namespace Bit.Tests.HtmlClient.ThirdPartyLibrariesTests.JayDataContextTests
{
    [TestClass]
    public class SyncTests
    {
        [TestMethod]
        [TestCategory("HtmlClient"), TestCategory("JayDataContextOData")]
        public virtual async Task TestSync()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment(new TestEnvironmentArgs { UseRealServer = true }))
            {
                TokenResponse token = await testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                using (RemoteWebDriver driver = testEnvironment.Server.BuildWebDriver(new RemoteWebDriverOptions { Token = token }))
                {
                    await driver.ExecuteTest("testSync");
                }

                List<TestCustomersController> testCustomersControllers = TestDependencyManager.CurrentTestDependencyManager.Objects
                    .OfType<TestCustomersController>()
                    .ToList();

                Assert.AreEqual(7, testCustomersControllers.Count);

                A.CallTo(() => testCustomersControllers.ElementAt(0).Create(A<TestCustomerDto>.That.Matches(c => c.Name == "A1"), A<CancellationToken>.Ignored))
                    .MustHaveHappened(Repeated.Exactly.Once);

                A.CallTo(() => testCustomersControllers.ElementAt(1).Create(A<TestCustomerDto>.That.Matches(c => c.Name == "A2"), A<CancellationToken>.Ignored))
                    .MustHaveHappened(Repeated.Exactly.Once);

                A.CallTo(() => testCustomersControllers.ElementAt(2).GetAll(A<CancellationToken>.Ignored))
                    .MustHaveHappened(Repeated.Exactly.Once);

                A.CallTo(() => testCustomersControllers.ElementAt(3).Create(A<TestCustomerDto>.That.Matches(c => c.Name == "A3"), A<CancellationToken>.Ignored))
                    .MustHaveHappened(Repeated.Exactly.Once);

                A.CallTo(() => testCustomersControllers.ElementAt(4).Create(A<TestCustomerDto>.That.Matches(c => c.Name == "A4"), A<CancellationToken>.Ignored))
                    .MustHaveHappened(Repeated.Exactly.Once);

                A.CallTo(() => testCustomersControllers.ElementAt(5).PartialUpdate(A<Guid>.Ignored, A<Delta<TestCustomerDto>>.That.Matches(c => c.GetInstance().Name == "A1?"), A<CancellationToken>.Ignored))
                    .MustHaveHappened(Repeated.Exactly.Once);

                A.CallTo(() => testCustomersControllers.ElementAt(6).GetAll(A<CancellationToken>.Ignored))
                    .MustHaveHappened(Repeated.Exactly.Once);
            }
        }
    }
}
