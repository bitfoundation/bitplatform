using Bit.Test;
using Bit.Test.Core.Implementations;
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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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

                Assert.AreEqual(9, testCustomersControllers.Count);

                A.CallTo(() => testCustomersControllers[0].Create(A<TestCustomerDto>.Ignored, A<CancellationToken>.Ignored))
                    .MustHaveHappened(Repeated.Exactly.Once);

                A.CallTo(() => testCustomersControllers[1].Create(A<TestCustomerDto>.Ignored, A<CancellationToken>.Ignored))
                    .MustHaveHappened(Repeated.Exactly.Once);

                A.CallTo(() => testCustomersControllers[2].GetAll(A<CancellationToken>.Ignored))
                    .MustHaveHappened(Repeated.Exactly.Once);

                A.CallTo(() => testCustomersControllers[3].Create(A<TestCustomerDto>.Ignored, A<CancellationToken>.Ignored))
                    .MustHaveHappened(Repeated.Exactly.Once);

                A.CallTo(() => testCustomersControllers[4].Create(A<TestCustomerDto>.Ignored, A<CancellationToken>.Ignored))
                    .MustHaveHappened(Repeated.Exactly.Once);

                A.CallTo(() => testCustomersControllers[5].PartialUpdate(A<Guid>.Ignored, A<Delta<TestCustomerDto>>.Ignored, A<CancellationToken>.Ignored))
                    .MustHaveHappened(Repeated.Exactly.Once);

                A.CallTo(() => testCustomersControllers[6].GetAll(A<CancellationToken>.Ignored))
                    .MustHaveHappened(Repeated.Exactly.Once);

                A.CallTo(() => testCustomersControllers[7].Delete(A<Guid>.Ignored, A<CancellationToken>.Ignored))
                    .MustHaveHappened(Repeated.Exactly.Once);

                A.CallTo(() => testCustomersControllers[8].GetAll(A<CancellationToken>.Ignored))
                    .MustHaveHappened(Repeated.Exactly.Once);
            }
        }
    }
}
